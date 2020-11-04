using System;
using System.Collections.Generic;
using System.Linq;
using ExactlyOnce.Routing.Controller.Model;
using NUnit.Framework;

namespace ExactlyOnce.Routing.Tests
{
    [TestFixture]
    public class EndToEndTests
    {
        [Test]
        public void Generates_routing_table()
        {



        }
    }

    [TestFixture]
    public class EndpointTests
    {
        [Test]
        public void It_publishes_handled_added_if_hello_is_received_first()
        {
            var receiverEndpoint = new Controller.Model.Endpoint("Receiver",
                new Dictionary<string, EndpointInstance>(),
                new Dictionary<string, MessageKind>());

            var changes = receiverEndpoint.OnHello("A", "Site").ToArray();

            Assert.IsEmpty(changes);

            changes = receiverEndpoint.OnStartup("A", new Dictionary<string, MessageKind>
            {
                ["Messages.MyMessage"] = MessageKind.Command
            }, new List<MessageHandlerInstance>
            {
                new MessageHandlerInstance("MyMessageHandler", "Messages.MyMessage")
            }).ToArray();

            var handlerAdded = changes.OfType<MessageHandlerAdded>().Single();
            var typeAdded = changes.OfType<MessageTypeAdded>().Single();

            Assert.AreEqual("Site", handlerAdded.Site);
            Assert.AreEqual("Receiver", handlerAdded.Endpoint);
            Assert.AreEqual("Messages.MyMessage", handlerAdded.HandledMessageType);
            Assert.AreEqual("MyMessageHandler", handlerAdded.HandlerType);

            Assert.AreEqual(MessageKind.Command, typeAdded.Kind);
            Assert.AreEqual("Messages.MyMessage", typeAdded.FullName);
        }

        [Test]
        public void It_publishes_handled_added_if_hello_is_received_second()
        {
            var receiverEndpoint = new Controller.Model.Endpoint("Receiver",
                new Dictionary<string, EndpointInstance>(),
                new Dictionary<string, MessageKind>());

            var changes = receiverEndpoint.OnStartup("A", new Dictionary<string, MessageKind>
            {
                ["Messages.MyMessage"] = MessageKind.Command
            }, new List<MessageHandlerInstance>
            {
                new MessageHandlerInstance("MyMessageHandler", "Messages.MyMessage")
            }).ToArray();

            Assert.IsFalse(changes.OfType<MessageHandlerAdded>().Any());
            var typeAdded = changes.OfType<MessageTypeAdded>().Single();
            Assert.AreEqual(MessageKind.Command, typeAdded.Kind);
            Assert.AreEqual("Messages.MyMessage", typeAdded.FullName);

            changes = receiverEndpoint.OnHello("A", "Site").ToArray();

            var handlerAdded = changes.OfType<MessageHandlerAdded>().Single();

            Assert.AreEqual("Site", handlerAdded.Site);
            Assert.AreEqual("Receiver", handlerAdded.Endpoint);
            Assert.AreEqual("Messages.MyMessage", handlerAdded.HandledMessageType);
            Assert.AreEqual("MyMessageHandler", handlerAdded.HandlerType);
        }

        [Test]
        public void It_can_correct_message_kind()
        {
            var receiverEndpoint = new Controller.Model.Endpoint("Receiver",
                new Dictionary<string, EndpointInstance>(),
                new Dictionary<string, MessageKind>());

            var changes = receiverEndpoint.OnStartup("A", new Dictionary<string, MessageKind>
            {
                ["Messages.MyMessage"] = MessageKind.Command
            }, new List<MessageHandlerInstance>
            {
                new MessageHandlerInstance("MyMessageHandler", "Messages.MyMessage")
            });

            var typeAdded = changes.OfType<MessageTypeAdded>().Single();

            Assert.AreEqual(MessageKind.Command, typeAdded.Kind);

            changes = receiverEndpoint.OnStartup("A", new Dictionary<string, MessageKind>
            {
                ["Messages.MyMessage"] = MessageKind.Event
            }, new List<MessageHandlerInstance>
            {
                new MessageHandlerInstance("MyMessageHandler", "Messages.MyMessage")
            });

            var kindChanged = changes.OfType<MessageKindChanged>().Single();

            Assert.AreEqual(MessageKind.Event, kindChanged.NewKind);
        }

        [Test]
        public void It_removes_handler_is_not_present_in_all_instances_of_a_site()
        {
            var receiverEndpoint = new Controller.Model.Endpoint("Receiver",
                new Dictionary<string, EndpointInstance>(),
                new Dictionary<string, MessageKind>());

            var changes = receiverEndpoint.OnStartup("A", new Dictionary<string, MessageKind>
            {
                ["Messages.MyMessage"] = MessageKind.Command
            }, new List<MessageHandlerInstance>
            {
                new MessageHandlerInstance("MyMessageHandler", "Messages.MyMessage")
            }).ToArray();

            changes = receiverEndpoint.OnStartup("B", new Dictionary<string, MessageKind>
            {
                ["Messages.MyMessage"] = MessageKind.Command
            }, new List<MessageHandlerInstance>()).ToArray();

            changes = receiverEndpoint.OnHello("A", "Site").ToArray();

            var handlerAdded = changes.OfType<MessageHandlerAdded>().Single();

            Assert.AreEqual("Site", handlerAdded.Site);
            Assert.AreEqual("Receiver", handlerAdded.Endpoint);
            Assert.AreEqual("Messages.MyMessage", handlerAdded.HandledMessageType);

            changes = receiverEndpoint.OnHello("B", "Site").ToArray();

            var handlerRemoved = changes.OfType<MessageHandlerRemoved>().Single();

            Assert.AreEqual("Site", handlerRemoved.Site);
            Assert.AreEqual("Receiver", handlerRemoved.Endpoint);
            Assert.AreEqual("Messages.MyMessage", handlerRemoved.HandledMessageType);
        }

        [Test]
        public void Does_not_remove_handler_if_not_present_in_instance_hosted_in_different_site()
        {
            var receiverEndpoint = new Controller.Model.Endpoint("Receiver",
                new Dictionary<string, EndpointInstance>(),
                new Dictionary<string, MessageKind>());

            var changes = receiverEndpoint.OnStartup("A", new Dictionary<string, MessageKind>
            {
                ["Messages.MyMessage"] = MessageKind.Command
            }, new List<MessageHandlerInstance>
            {
                new MessageHandlerInstance("MyMessageHandler", "Messages.MyMessage")
            }).ToArray();

            changes = receiverEndpoint.OnStartup("B", new Dictionary<string, MessageKind>
            {
                ["Messages.MyMessage"] = MessageKind.Command
            }, new List<MessageHandlerInstance>()).ToArray();

            changes = receiverEndpoint.OnHello("A", "SiteA").ToArray();

            var handlerAdded = changes.OfType<MessageHandlerAdded>().Single();

            Assert.AreEqual("SiteA", handlerAdded.Site);
            Assert.AreEqual("Receiver", handlerAdded.Endpoint);
            Assert.AreEqual("Messages.MyMessage", handlerAdded.HandledMessageType);

            changes = receiverEndpoint.OnHello("B", "SiteB").ToArray();
            Assert.IsEmpty(changes);
        }
    }
}
