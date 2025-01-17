﻿using System;
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
    public class When_sending_a_command : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_deliver_it_to_appointed_destination()
        {
            var result = await Scenario.Define<Context>()
                .WithController()
                .WithRouter("Router", "a", cfg =>
                {
                    cfg.AddInterface<TestTransport>("Alpha", t => t.BrokerAlpha());
                    cfg.AddInterface<TestTransport>("Bravo", t => t.BrokerBravo());
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
                .WithManagedEndpoint<Context, Receiver>("a", "Router")
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
                .Do("Query state", async (context, client) =>
                {
                    var list = await client.ListEndpoints("SendingACommand");
                    Assert.AreEqual(2, list.Items.Count);

                    var senderInfoByName = await client.GetEndpoint(list.Items.First(x => x.Name.Contains("Sender")).Name);
                    var receiverInfoById = await client.GetEndpoint(list.Items.First(x => x.Name.Contains("Receiver")).Id);

                    Assert.AreEqual("SendingACommand.Sender", senderInfoByName.Name);
                    var senderInstance = senderInfoByName.Instances.Single().Value;
                    Assert.AreEqual("Alpha", senderInstance.Site);

                    Assert.AreEqual("SendingACommand.Receiver", receiverInfoById.Name);
                    var receiverInstance = receiverInfoById.Instances.Single().Value;
                    Assert.AreEqual("Bravo", receiverInstance.Site);
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
                    c.UseTransport<TestTransport>().BrokerBravo();
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
