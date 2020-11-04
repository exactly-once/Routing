using System.Collections.Generic;
using System.Linq;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using NUnit.Framework;

namespace ExactlyOnce.Routing.Tests
{
    [TestFixture]
    public class EventDrivenStateTests
    {
        [Test]
        public void Can_invoke_handler_method()
        {
            var state = new MyEventDrivenState
            {
                Data = new HandlerObject()
            };

            var subs = new Subscriptions();
            subs.Subscribe<MyEventDrivenState, FollowUpEvent>(x => "ID");
            var result = state.OnEvent(new EventMessage("message-id", null, null, "dest", "type", new MyEvent()), subs).ToArray();

            var followUpMessage = result.Single();
            var followUpPayload = followUpMessage.Payload as FollowUpEvent;

            Assert.IsNotNull(followUpPayload);
        }

        public class MyEventDrivenState : State<HandlerObject>
        {
        }

        public class HandlerObject : IEventHandler<MyEvent>
        {
            public IEnumerable<IEvent> Handle(MyEvent e)
            {
                yield return new FollowUpEvent();
            }
        }

        public class MyEvent : IEvent
        {
        }

        public class FollowUpEvent : IEvent
        {
        }
    }
}