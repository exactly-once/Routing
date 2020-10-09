using System.Collections.Generic;
using System.Linq;
using ExactlyOnce.Routing.Controller.Model;
using NUnit.Framework;

namespace ExactlyOnce.Routing.Tests
{
    [TestFixture]
    public class MessageRoutingTests
    {
        [Test]
        public void Can_appoint_command_handler()
        {
            var messageRouting = new MessageRouting("Messages.MyMessage", new List<Destination>());

            var events = messageRouting.HandlerAdded("MyMessageHandler", "SiteA", "Receiver", MessageKind.Command).ToArray();

            Assert.IsEmpty(events);

            events = messageRouting.Appoint("MyMessageHandler", "Receiver").ToArray();

            var routeAdded = events.OfType<RouteAdded>().Single();

            Assert.AreEqual("MyMessageHandler", routeAdded.HandlerType);
            Assert.AreEqual("Receiver", routeAdded.Endpoint);
            Assert.AreEqual("Messages.MyMessage", routeAdded.MessageType);
            CollectionAssert.Contains(routeAdded.Sites, "SiteA");
        }

        [Test]
        public void Can_dismiss_command_handler()
        {
            var messageRouting = new MessageRouting("Messages.MyMessage", new List<Destination>
            {
                new Destination("MyMessageHandler", "Receiver", DestinationState.Active, MessageKind.Command, new List<string> {"SiteA"})
            });

            var events = messageRouting.Dismiss("MyMessageHandler", "Receiver").ToArray();

            var routeAdded = events.OfType<RouteRemoved>().Single();

            Assert.AreEqual("MyMessageHandler", routeAdded.HandlerType);
            Assert.AreEqual("Receiver", routeAdded.Endpoint);
            Assert.AreEqual("Messages.MyMessage", routeAdded.MessageType);
        }

        [Test]
        public void Appointing_command_handler_dismisses_previous_handler()
        {
            var messageRouting = new MessageRouting("Messages.MyMessage", new List<Destination>
            {
                new Destination("MyMessageHandler", "Receiver", DestinationState.Active, MessageKind.Command, new List<string> {"SiteA"}),
                new Destination("AnotherMessageHandler", "Receiver", DestinationState.Inactive, MessageKind.Command, new List<string> {"SiteA"})
            });

            var events = messageRouting.Appoint("AnotherMessageHandler", "Receiver").ToArray();

            var routeAdded = events.OfType<RouteAdded>().Single();
            var routeRemoved = events.OfType<RouteRemoved>().Single();

            Assert.AreEqual("MyMessageHandler", routeRemoved.HandlerType);
            Assert.AreEqual("Receiver", routeRemoved.Endpoint);
            Assert.AreEqual("Messages.MyMessage", routeRemoved.MessageType);

            Assert.AreEqual("AnotherMessageHandler", routeAdded.HandlerType);
            Assert.AreEqual("Receiver", routeAdded.Endpoint);
            Assert.AreEqual("Messages.MyMessage", routeAdded.MessageType);
            CollectionAssert.Contains(routeAdded.Sites, "SiteA");
        }

        [Test]
        public void Appointing_command_handler_dismisses_same_handler_in_different_endpoint()
        {
            var messageRouting = new MessageRouting("Messages.MyMessage", new List<Destination>
            {
                new Destination("MyMessageHandler", "Receiver", DestinationState.Active, MessageKind.Command, new List<string> {"SiteA"}),
                new Destination("MyMessageHandler", "NewReceiver", DestinationState.Inactive, MessageKind.Command, new List<string> {"SiteA"})
            });

            var events = messageRouting.Appoint("MyMessageHandler", "NewReceiver").ToArray();

            var routeAdded = events.OfType<RouteAdded>().Single();
            var routeRemoved = events.OfType<RouteRemoved>().Single();

            Assert.AreEqual("MyMessageHandler", routeRemoved.HandlerType);
            Assert.AreEqual("Receiver", routeRemoved.Endpoint);
            Assert.AreEqual("Messages.MyMessage", routeRemoved.MessageType);

            Assert.AreEqual("MyMessageHandler", routeAdded.HandlerType);
            Assert.AreEqual("NewReceiver", routeAdded.Endpoint);
            Assert.AreEqual("Messages.MyMessage", routeAdded.MessageType);
            CollectionAssert.Contains(routeAdded.Sites, "SiteA");
        }

        [Test]
        public void Subscribing_event_handler_unsubscribes_same_handler_in_different_endpoint()
        {
            var messageRouting = new MessageRouting("Messages.MyMessage", new List<Destination>
            {
                new Destination("MyMessageHandler", "Receiver", DestinationState.Active, MessageKind.Event, new List<string> {"SiteA"}),
                new Destination("MyMessageHandler", "NewReceiver", DestinationState.Inactive, MessageKind.Event, new List<string> {"SiteA"})
            });

            var events = messageRouting.Subscribe("MyMessageHandler", "NewReceiver").ToArray();

            var routeAdded = events.OfType<RouteAdded>().Single();
            var routeRemoved = events.OfType<RouteRemoved>().Single();

            Assert.AreEqual("MyMessageHandler", routeRemoved.HandlerType);
            Assert.AreEqual("Receiver", routeRemoved.Endpoint);
            Assert.AreEqual("Messages.MyMessage", routeRemoved.MessageType);

            Assert.AreEqual("MyMessageHandler", routeAdded.HandlerType);
            Assert.AreEqual("NewReceiver", routeAdded.Endpoint);
            Assert.AreEqual("Messages.MyMessage", routeAdded.MessageType);
            CollectionAssert.Contains(routeAdded.Sites, "SiteA");
        }

        [Test]
        public void Subscribing_event_handler_does_not_unsubscribe_other_handlers()
        {
            var messageRouting = new MessageRouting("Messages.MyMessage", new List<Destination>
            {
                new Destination("MyMessageHandler", "Sub1", DestinationState.Active, MessageKind.Event, new List<string> {"SiteA"}),
                new Destination("AnotherMessageHandler", "Sub2", DestinationState.Inactive, MessageKind.Event, new List<string> {"SiteA"})
            });

            var events = messageRouting.Subscribe("AnotherMessageHandler", "Sub2").ToArray();

            var routeAdded = (RouteAdded)events.Single();

            Assert.AreEqual("AnotherMessageHandler", routeAdded.HandlerType);
            Assert.AreEqual("Sub2", routeAdded.Endpoint);
            Assert.AreEqual("Messages.MyMessage", routeAdded.MessageType);
            CollectionAssert.Contains(routeAdded.Sites, "SiteA");
        }
    }
}