using System;
using System.Collections.Generic;
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
    public class When_migrating_subscriber_first_message_driven : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_continue_delivering_events_to_subscribers()
        {
            var result = await Scenario.Define<Context>(x => x.Stage = "Before migration")
                .WithEndpoint<Publisher>(b => b
                    .When(c => c.Subscribed, s => s.Publish(new MyEvent())))
                .WithEndpoint<Subscriber>()
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(result.EventReceived);

            result = await Scenario.Define<Context>(x => x.Stage = "Subscriber migrated")
                .WithController()
                .WithRouter("Router", "a", cfg =>
                {
                    cfg.AddInterface<TestTransport>("Bravo", t => t.BrokerBravo());
                })
                .WithEndpoint<Publisher>(b => b
                    .When(c => c.EndpointsStarted, s => s.Publish(new MyEvent())))
                .WithManagedEndpoint<Context, Subscriber>("a", "Router", b => b
                    .CustomConfig(cfg =>
                    {
                        var routing = cfg.GetSettings().Get<ExactlyOnceRoutingSettings>();
                        routing.EnableLegacyMigrationMode();
                    }))
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(result.EventReceived);


            result = await Scenario.Define<Context>(x => x.Stage = "Publisher migrated")
                .WithController()
                .WithRouter("Router", "a", cfg =>
                {
                    cfg.AddInterface<TestTransport>("Bravo", t => t.BrokerBravo());
                })
                .WithManagedEndpoint<Context, Publisher>("a", "Router", b => b
                    .CustomConfig(cfg =>
                    {
                        var routing = cfg.GetSettings().Get<ExactlyOnceRoutingSettings>();
                        routing.EnableLegacyMigrationMode();
                    })
                    .When(c => c.EndpointsStarted, async (s, c) =>
                    {
                        while (!c.MessageWithRoutingHeadersReceived)
                        {
                            try
                            {
                                await s.Publish(new MyEvent());
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
                .WithManagedEndpoint<Context, Subscriber>("a", "Router", b => b
                    .CustomConfig(cfg =>
                    {
                        var routing = cfg.GetSettings().Get<ExactlyOnceRoutingSettings>();
                        routing.EnableLegacyMigrationMode();
                    }))
                .Done(c => c.MessageWithRoutingHeadersReceived)
                .Run();

            Assert.IsTrue(result.EventReceived);
            Assert.IsTrue(result.MessageWithRoutingHeadersReceived);
        }

        class Context : ScenarioContext, ISequenceContext
        {
            public string Stage { get; set; }
            public bool EventReceived { get; set; }
            public int Step { get; set; }
            public bool MessageWithRoutingHeadersReceived { get; set; }
            public bool Subscribed { get; set; }
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<TestTransport>().BrokerBravo();
                    c.OnEndpointSubscribed<Context>((args, ctx) =>
                    {
                        ctx.Subscribed = true;
                    });
                });
            }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var routing = c.UseTransport<TestTransport>().BrokerBravo().Routing();
                    routing.RegisterPublisher(typeof(MyEvent), Conventions.EndpointNamingConvention(typeof(Publisher)));
                });
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                readonly Context scenarioContext;

                public MyEventHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(MyEvent e, IMessageHandlerContext context)
                {
                    scenarioContext.EventReceived = true;
                    if (context.MessageHeaders.ContainsKey("ExactlyOnce.Routing.DestinationEndpoint"))
                    {
                        scenarioContext.MessageWithRoutingHeadersReceived = true;
                    }
                    return Task.CompletedTask;
                }
            }
        }

        class MyEvent : IEvent
        {
        }
    }
}
