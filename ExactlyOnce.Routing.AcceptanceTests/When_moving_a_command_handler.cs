using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Settings;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

namespace ExactlyOnce.Routing.AcceptanceTests
{
    [TestFixture]
    public class When_moving_a_command_handler : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_deliver_commands_to_new_destination_destination()
        {
            var result = await Scenario.Define<Context>()
                .WithController()
                .WithRouter("Router", "a", cfg =>
                {
                    cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha());
                    cfg.AddInterface<TestTransport>("Bravo", t => t.BrokerBravo());
                })
                .WithManagedEndpoint<Context, Sender>("a", "Router", c => 
                    c.When(c => c.OldHandlerAppointed, async s =>
                    {
                        while (true)
                        {
                            try
                            {
                                await s.Send(new MyRequest());
                                break;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                await Task.Delay(1000);
                            }

                        }
                    }).When(c => c.NewHandlerAppointed, async (s,ctx) =>
                    {
                        while (!ctx.RequestReceivedByNewEndpoint) //continue sending until the routing flips
                        {
                            try
                            {
                                await s.Send(new MyRequest());
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            finally
                            {
                                await Task.Delay(1000);
                            }

                        }
                    }))
                .WithManagedEndpoint<Context, OldReceiver>("a", "Router")
                .WithManagedEndpoint<Context, NewReceiver>("a", "Router")
                .Do("Wait for message handlers to be registered", async (ctx, client) =>
                {
                    var handlers = await client.GetDestinations(typeof(MyRequest).FullName);
                    if (handlers == null || handlers.Destinations.Count(x =>
                        x.HandlerType.Contains(nameof(MyRequestHandler))) < 2) //Both endpoints register their destinations
                    {
                        return false;
                    }

                    return true;

                })
                .Do("Appoint old handler", async (context, client) =>
                {
                    await client.Appoint(Conventions.EndpointNamingConvention(typeof(OldReceiver)), typeof(MyRequestHandler),
                        typeof(MyRequest), Guid.NewGuid().ToString()).ConfigureAwait(false);
                    context.OldHandlerAppointed = true;
                })
                .Do("Wait for old handler to process the message", async (context, client) =>
                {
                    if (!context.RequestReceivedByOldEndpoint)
                    {
                        return false;
                    }
                    return true;
                })
                .Do("Appoint new handler", async (context, client) =>
                {
                    await client.Appoint(Conventions.EndpointNamingConvention(typeof(NewReceiver)), typeof(MyRequestHandler),
                        typeof(MyRequest), Guid.NewGuid().ToString()).ConfigureAwait(false);
                    context.NewHandlerAppointed = true;
                })
                .Done(c => c.RequestReceivedByNewEndpoint)
                .Run();

            Assert.IsTrue(result.RequestReceivedByNewEndpoint);
        }

        class Context : ScenarioContext, ISequenceContext
        {
            public int Step { get; set; }
            public bool NewHandlerAppointed { get; set; }
            public bool RequestReceivedByNewEndpoint { get; set; }
            public bool OldHandlerAppointed { get; set; }
            public bool RequestReceivedByOldEndpoint { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<TestTransport>().BrokerAlpha();
                }).ExcludeType<MyRequestHandler>();
            }
        }

        class OldReceiver : EndpointConfigurationBuilder
        {
            public OldReceiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<TestTransport>().BrokerBravo();
                });
            }
        }

        class NewReceiver : EndpointConfigurationBuilder
        {
            public NewReceiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<TestTransport>().BrokerBravo();
                });
            }
        }

        class MyRequestHandler : IHandleMessages<MyRequest>
        {
            readonly Context scenarioContext;
            readonly ReadOnlySettings settings;

            public MyRequestHandler(Context scenarioContext, ReadOnlySettings settings)
            {
                this.scenarioContext = scenarioContext;
                this.settings = settings;
            }

            public Task Handle(MyRequest request, IMessageHandlerContext context)
            {
                if (settings.EndpointName().Contains("NewReceiver"))
                {
                    scenarioContext.RequestReceivedByNewEndpoint = true;
                }
                if (settings.EndpointName().Contains("OldReceiver"))
                {
                    scenarioContext.RequestReceivedByOldEndpoint = true;
                }

                return Task.CompletedTask;
            }
        }

        class MyRequest : IMessage
        {
        }
    }
}
