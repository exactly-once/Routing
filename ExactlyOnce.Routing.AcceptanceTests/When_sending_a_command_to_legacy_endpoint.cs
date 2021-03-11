using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.Routing.ApiContract;
using ExactlyOnce.Routing.Client;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

namespace ExactlyOnce.Routing.AcceptanceTests
{
    [TestFixture]
    public class When_sending_a_command_to_legacy_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_deliver_it_to_appointed_destination()
        {
            var instanceA = DeterministicGuid.MakeId("a");

            var result = await Scenario.Define<Context>()
                .WithController()
                .WithRouter("Router", "a", cfg =>
                {
                    cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha());
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
                .WithEndpoint<Receiver>(b => 
                    b.CustomConfig(cfg => cfg.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(instanceA)))
                .Do("Register the legacy endpoint", async (ctx, client) =>
                {
                    var receiverEndpointName = Conventions.EndpointNamingConvention(typeof(Receiver));

                    await client.RegisterEndpoint(receiverEndpointName,
                        instanceA.ToString(),
                        $"{receiverEndpointName}@Alpha",
                        new Dictionary<string, MessageKind>
                        {
                            {typeof(MyRequest).FullName, MessageKind.Command}
                        },
                        new Dictionary<string, string>
                        {
                            {typeof(Receiver.MyRequestHandler).ToHandlerTypeName(), typeof(MyRequest).FullName}
                        },
                        new Dictionary<string, string>(), 
                        false,
                        Guid.NewGuid().ToString());

                    await client.RegisterEndpointSite(receiverEndpointName, 
                        instanceA.ToString(), 
                        "Alpha",
                        Guid.NewGuid().ToString());
                    
                    return true;
                })
                .Do("Wait for message handler to be registered", async (ctx, client) =>
                {
                    var handlers = await client.GetMessageType(typeof(MyRequest).FullName);
                    if (handlers == null || !handlers.Destinations.Any(x =>
                        x.HandlerType.Contains(nameof(Receiver.MyRequestHandler))))
                    {
                        return false;
                    }

                    return true;
                })
                .Do("Appoint handler", async (context, client) =>
                {
                    await client.Appoint(Conventions.EndpointNamingConvention(typeof(Receiver)), typeof(Receiver.MyRequestHandler),
                        typeof(MyRequest), Guid.NewGuid().ToString()).ConfigureAwait(false);
                    context.HandlerAppointed = true;
                })
                .Done(c => c.RequestReceived)
                .Run();

            Assert.IsTrue(result.RequestReceived);
        }

        class Context : ScenarioContext, ISequenceContext
        {
            public bool RequestReceived { get; set; }
            public int Step { get; set; }
            public bool HandlerAppointed { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<TestTransport>().BrokerAlpha();
                });
            }
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<TestTransport>().BrokerAlpha();
                });
            }

            public class MyRequestHandler : IHandleMessages<MyRequest>
            {
                Context scenarioContext;

                public MyRequestHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(MyRequest request, IMessageHandlerContext context)
                {
                    scenarioContext.RequestReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class MyRequest : IMessage
        {
        }
    }
}
