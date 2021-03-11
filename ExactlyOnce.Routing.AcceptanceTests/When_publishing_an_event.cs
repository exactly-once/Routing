using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;
using Conventions = NServiceBus.AcceptanceTesting.Customization.Conventions;

namespace ExactlyOnce.Routing.AcceptanceTests
{
    [TestFixture]
    public class When_publishing_an_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_deliver_it_to_the_subscriber()
        {
            var result = await Scenario.Define<Context>()
                .WithController()
                .WithRouter("Router", "a", cfg =>
                {
                    cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha());
                    cfg.AddInterface<TestTransport>("Bravo", t => t.BrokerBravo());
                })
                .WithManagedEndpoint<Context, Publisher>("a", "Router", c => 
                    c.When(c => c.HandlerSubscribed, async (s, ctx) =>
                    {
                        while (!ctx.EventReceived)
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
                .WithManagedEndpoint<Context, Subscriber>("a", "Router")
                .Do("Wait for message handler to be registered", async (ctx, client) =>
                {
                    var handlers = await client.GetMessageType(typeof(MyEvent).FullName);
                    if (handlers == null || !handlers.Destinations.Any(x =>
                        x.HandlerType.Contains(nameof(Subscriber.MyEventHandler))))
                    {
                        return false;
                    }

                    return true;

                })
                .Do("Subscribe", async (context, client) =>
                {
                    await client.Subscribe(Conventions.EndpointNamingConvention(typeof(Subscriber)), typeof(Subscriber.MyEventHandler), null,
                        typeof(MyEvent), Guid.NewGuid().ToString()).ConfigureAwait(false);
                    context.HandlerSubscribed = true;
                })
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(result.EventReceived);
        }

        class Context : ScenarioContext, ISequenceContext
        {
            public bool EventReceived { get; set; }
            public int Step { get; set; }
            public bool HandlerSubscribed { get; set; }
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<TestTransport>().BrokerAlpha();
                });
            }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UseTransport<TestTransport>().BrokerBravo();
                });
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                Context scenarioContext;

                public MyEventHandler(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public Task Handle(MyEvent request, IMessageHandlerContext context)
                {
                    scenarioContext.EventReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class MyEvent : IEvent
        {
        }
    }
}
