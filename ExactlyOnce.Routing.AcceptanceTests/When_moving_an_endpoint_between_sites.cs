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
    public class When_moving_an_endpoint_between_sites : NServiceBusAcceptanceTest
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
                    cfg.AddInterface<TestTransport>("Charlie", t => t.BrokerCharlie());
                })
                .WithManagedEndpoint<Context, Sender>("a", "Router", c => 
                    c.When(c => c.HandlerAppointed, async s =>
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
                    }))
                .WithManagedEndpoint<Context, Receiver>("a", "Router", b =>
                    b.CustomConfig(cfg => cfg.UseTransport<TestTransport>().BrokerBravo()))
                .Do("Wait for message handlers to be registered", async (ctx, client) =>
                {
                    var handlers = await client.GetMessageType(typeof(MyRequest).FullName);
                    if (handlers == null || handlers.Destinations.Count(x =>
                        x.HandlerType.Contains(nameof(MyRequestHandler))) == 0)
                    {
                        return false;
                    }

                    return true;

                })
                .Do("Appoint handler", async (context, client) =>
                {
                    await client.Appoint(Conventions.EndpointNamingConvention(typeof(Receiver)), typeof(MyRequestHandler),
                        typeof(MyRequest), Guid.NewGuid().ToString()).ConfigureAwait(false);
                    context.HandlerAppointed = true;
                })
                .Done(c => c.RequestReceivedInOldSite)
                .Run();

            Assert.IsTrue(result.RequestReceivedInOldSite);

            result = await Scenario.Define<Context>()
                .WithController()
                .WithRouter("Router", "a", cfg =>
                {
                    cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha());
                    cfg.AddInterface<TestTransport>("Bravo", t => t.BrokerBravo());
                    cfg.AddInterface<TestTransport>("Charlie", t => t.BrokerCharlie());
                })
                .WithManagedEndpoint<Context, Sender>("a", "Router", c =>
                    c.When(c => c.EndpointsStarted, async (s, ctx) =>
                    {
                        while (!ctx.RequestReceivedInNewSite)
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
                .WithManagedEndpoint<Context, Receiver>("b", "Router", b => 
                    b.CustomConfig(cfg => cfg.UseTransport<TestTransport>().BrokerCharlie()))
                .Done(c => c.RequestReceivedInNewSite)
                .Run();

            Assert.IsTrue(result.RequestReceivedInNewSite);
        }

        class Context : ScenarioContext, ISequenceContext
        {
            public int Step { get; set; }
            public bool RequestReceivedInNewSite { get; set; }
            public bool HandlerAppointed { get; set; }
            public bool RequestReceivedInOldSite { get; set; }
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

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
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
                if (settings.LocalAddress().Contains("Charlie"))
                {
                    scenarioContext.RequestReceivedInNewSite = true;
                }
                if (settings.LocalAddress().Contains("Bravo"))
                {
                    scenarioContext.RequestReceivedInOldSite = true;
                }

                return Task.CompletedTask;
            }
        }

        class MyRequest : IMessage
        {
        }
    }
}
