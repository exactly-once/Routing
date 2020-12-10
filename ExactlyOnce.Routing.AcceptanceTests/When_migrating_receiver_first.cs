using System;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Client;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Configuration.AdvancedExtensibility;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

namespace ExactlyOnce.Routing.AcceptanceTests
{
    [TestFixture]
    public class When_migrating_receiver_first : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_continue_delivering_commands_to_appointed_destination()
        {
            var result = await Scenario.Define<Context>(x => x.Stage = "Before migration")
                .WithEndpoint<Sender>(b => b.CustomConfig(cfg =>
                {
                    var transport = cfg.UseTransport<TestTransport>();
                    var routing = transport.Routing();
                    routing.RouteToEndpoint(typeof(MyRequest), Conventions.EndpointNamingConvention(typeof(Receiver)));
                }).When(s => s.Send(new MyRequest())))
                .WithEndpoint<Receiver>()
                .Done(c => c.RequestReceived)
                .Run();

            result = await Scenario.Define<Context>(x => x.Stage = "Receiver migrated")
                .WithController()
                .WithRouter("Router", "a", cfg => { cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha()); })
                .WithEndpoint<Sender>(b => b.CustomConfig(cfg =>
                {
                    var transport = cfg.UseTransport<TestTransport>();
                    var routing = transport.Routing();
                    routing.RouteToEndpoint(typeof(MyRequest), Conventions.EndpointNamingConvention(typeof(Receiver)));
                }).When(s => s.Send(new MyRequest())))
                .WithManagedEndpoint<Context, Receiver>("a", "Router")
                .Do("Wait for routing table to be updated", async (context, client) =>
                {
                    var routingTable = await client.GetRoutingTable();

                    var receiverInstances = routingTable.Sites.Values
                        .SelectMany(x => x).Where(x =>
                            x.EndpointName == Conventions.EndpointNamingConvention(typeof(Receiver)));

                    context.ReceiverInstances = receiverInstances.ToArray();
                    context.RoutingTable = routingTable;

                    return context.ReceiverInstances.Any(x => x.InstanceId == DeterministicGuid.MakeId("a").ToString());
                })
                .Done()
                .Run();

            Assert.IsTrue(result.RequestReceived);

            result = await Scenario.Define<Context>(x => x.Stage = "Sender migrated")
                .WithController()
                .WithRouter("Router", "a", cfg => { cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha()); })
                .WithManagedEndpoint<Context, Sender>("a", "Router", b => b.CustomConfig(cfg =>
                {
                    var routing = cfg.GetSettings().Get<ExactlyOnceRoutingSettings>();
                    routing.EnableLegacyMigrationMode().RouteToEndpoint(typeof(MyRequest),
                        Conventions.EndpointNamingConvention(typeof(Receiver)));
                }).When(c => c.EndpointsStarted, s => s.Send(new MyRequest())))
                .WithManagedEndpoint<Context, Receiver>("a", "Router")
                .Done(c => c.RequestReceived)
                .Run();

            Assert.IsTrue(result.RequestReceived);

            result = await Scenario.Define<Context>(x => x.Stage = "Legacy routing information removed")
                .WithController()
                .WithRouter("Router", "a", cfg => { cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha()); })
                .WithManagedEndpoint<Context, Sender>("a", "Router", b => b.When(c => c.EndpointsStarted, async s =>
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
                .WithManagedEndpoint<Context, Receiver>("a", "Router")
                .Done(c => c.RequestReceived)
                .Run();

            Assert.IsTrue(result.RequestReceived);
        }

        class Context : ScenarioContext, ISequenceContext
        {
            public string Stage { get; set; }
            public bool RequestReceived { get; set; }
            public int Step { get; set; }
            public EndpointInstanceId[] ReceiverInstances { get; set; }
            public RoutingTable RoutingTable { get; set; }
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
