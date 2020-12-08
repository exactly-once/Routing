using System;
using System.Threading.Tasks;
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
    public class When_migrating_a_command_handler : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_continue_delivering_commands_to_appointed_destination()
        {
            var result = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b => b.CustomConfig(cfg =>
                {
                    var transport = cfg.UseTransport<TestTransport>();
                    var routing = transport.Routing();
                    routing.RouteToEndpoint(typeof(MyRequest), Conventions.EndpointNamingConvention(typeof(Receiver)));
                }).When(s => s.Send(new MyRequest())))
                .WithEndpoint<Receiver>()
                .Done(c => c.RequestReceived)
                .Run();

            //migrate sender
            result = await Scenario.Define<Context>()
                .WithController()
                .WithRouter("Router", "a", cfg =>
                {
                    cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha());
                })
                .WithManagedEndpoint<Context, Sender>("a", "Router", b => b.CustomConfig(cfg =>
                {
                    var routing = cfg.GetSettings().Get<ExactlyOnceRoutingSettings>();
                    routing.LegacyMigration.RouteToEndpoint(typeof(MyRequest), Conventions.EndpointNamingConvention(typeof(Receiver)));
                }).When(c => c.EndpointsStarted, s => s.Send(new MyRequest())))
                .WithEndpoint<Receiver>()
                .Done(c => c.RequestReceived)
                .Run();

            Assert.IsTrue(result.RequestReceived);

            //legacy routing information removed from code -- should be present by now in the routing table
            result = await Scenario.Define<Context>()
                .WithController()
                .WithRouter("Router", "a", cfg =>
                {
                    cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha());
                })
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
                .WithEndpoint<Receiver>()
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
