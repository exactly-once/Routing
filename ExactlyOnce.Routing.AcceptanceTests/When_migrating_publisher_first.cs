using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Pipeline;
using NUnit.Framework;

namespace ExactlyOnce.Routing.AcceptanceTests
{
    [TestFixture]
    public class When_migrating_publisher_first : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_continue_delivering_events_to_subscribers()
        {
            var result = await Scenario.Define<Context>(x => x.Stage = "Before migration")
                .WithEndpoint<Publisher>(b => b
                    .When(c => c.EndpointsStarted, s => s.Publish(new MyEvent())))
                .WithEndpoint<Subscriber>()
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(result.EventReceived);

            result = await Scenario.Define<Context>(x => x.Stage = "Publisher migrated")
                .WithController()
                .WithRouter("Router", "a", cfg =>
                {
                    cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha());
                })
                .WithManagedEndpoint<Context, Publisher>("a", "Router", b => b
                    .CustomConfig(cfg =>
                    {
                        var routing = cfg.GetSettings().Get<ExactlyOnceRoutingSettings>();
                        routing.EnableLegacyMigrationMode();
                    })
                    .When(c => c.EndpointsStarted, s => s.Publish(new MyEvent())))
                .WithEndpoint<Subscriber>()
                .Done(c => c.EventReceived)
                .Run();

            Assert.IsTrue(result.EventReceived);

            result = await Scenario.Define<Context>(x => x.Stage = "Subscriber migrated")
                .WithController()
                .WithRouter("Router", "a", cfg =>
                {
                    cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha());
                })
                .WithManagedEndpoint<Context, Publisher>("a", "Router", b => b
                    .CustomConfig(cfg =>
                    {
                        var routing = cfg.GetSettings().Get<ExactlyOnceRoutingSettings>();
                        routing.EnableLegacyMigrationMode();
                    })
                    .When(c => c.EndpointsStarted, async (s, c) =>
                    {
                        while (c.TwoCopiesMessageId == null)
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
                .Done(c => c.TwoCopiesMessageId != null)
                .Run();

            Assert.IsTrue(result.EventReceived);
            Assert.IsNotNull(result.TwoCopiesMessageId);

            var receivedCount = result.NumbersOfMessageCopiesReceived[result.TwoCopiesMessageId];
            var processedCount = result.NumbersOfMessageCopiesProcessed[result.TwoCopiesMessageId];

            //We should receive two copies of event
            Assert.AreEqual(2, receivedCount);

            //But one of them should be discarded as duplicate
            Assert.AreEqual(1, processedCount);
        }

        class Context : ScenarioContext, ISequenceContext
        {
            public string Stage { get; set; }
            public bool EventReceived { get; set; }
            public int Step { get; set; }
            public Dictionary<string, int> NumbersOfMessageCopiesReceived { get; } = new Dictionary<string, int>();
            public Dictionary<string, int> NumbersOfMessageCopiesProcessed { get; } = new Dictionary<string, int>();
            public string TwoCopiesMessageId { get; set; }
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
                    c.UseTransport<TestTransport>().BrokerAlpha();
                    c.Pipeline.Register(b => new ReceivedMessageCounter(b.Build<Context>()), "Counts copies of received messages");
                    c.Pipeline.Register(b => new ProcessedMessageCounter(b.Build<Context>()), "Counts copies of processed messages");
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
                    return Task.CompletedTask;
                }
            }

            public class ReceivedMessageCounter : Behavior<ITransportReceiveContext>
            {
                readonly Context scenarioContext;

                public ReceivedMessageCounter(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
                {
                    await next();

                    var received = scenarioContext.NumbersOfMessageCopiesReceived;
                    lock (received)
                    {
                        received.TryGetValue(context.Message.MessageId, out var currentNumber);
                        currentNumber++;
                        received[context.Message.MessageId] = currentNumber;
                        if (currentNumber == 2)
                        {
                            scenarioContext.TwoCopiesMessageId = context.Message.MessageId;
                        }
                    }
                }
            }

            public class ProcessedMessageCounter : Behavior<IIncomingLogicalMessageContext>
            {
                readonly Context scenarioContext;

                public ProcessedMessageCounter(Context scenarioContext)
                {
                    this.scenarioContext = scenarioContext;
                }

                public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
                {
                    await next();

                    var processed = scenarioContext.NumbersOfMessageCopiesProcessed;
                    lock (processed)
                    {
                        processed.TryGetValue(context.MessageId, out var currentNumber);
                        currentNumber++;
                        processed[context.MessageId] = currentNumber;
                    }
                }
            }
        }

        class MyEvent : IEvent
        {
        }
    }
}
