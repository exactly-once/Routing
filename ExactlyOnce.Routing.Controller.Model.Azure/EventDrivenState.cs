using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public abstract class EventDrivenState : State
    {
        protected EventDrivenState(Inbox inbox, Outbox outbox, object eventHandler, string id)
            : base(DeterministicGuid.MakeId(id).ToString())
        {
            this.eventHandler = eventHandler;
            Inbox = inbox;
            Outbox = outbox;
        }

        public Inbox Inbox { get; set; }
        public Outbox Outbox { get; set; }
        public object Data { get; set; }

        public IEnumerable<EventMessage> OnEvent(EventMessage eventMessage, Subscriptions subscriptions)
        {
            var generatedEvents = Inbox.AppendAndProcess(eventMessage, Process);
            return subscriptions.ToMessages(generatedEvents, Outbox);
        }

        IEnumerable<IEvent> Process(IEvent e)
        {
            var payloadType = e.GetType();
            var handleInterfaceType = typeof(IEventHandler<>).MakeGenericType(payloadType);
            var interfaces = eventHandler.GetType().GetInterfaces();
            if (!interfaces.Contains(handleInterfaceType))
            {
                throw new Exception($"Type {this.GetType().FullName} cannot handle events of type {payloadType.FullName}");
            }

            var resultEnumerable = (IEnumerable<IEvent>)handleInterfaceType.InvokeMember("Handle",
                BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                null, eventHandler,
                new object[] { e });
            return resultEnumerable;
        }
    }

    public abstract class EventDrivenState<T> : EventDrivenState
    {
        readonly T actionHandler;

        protected EventDrivenState(Inbox inbox, Outbox outbox, T eventHandler, string id)
            : base(inbox, outbox, eventHandler, id)
        {
            actionHandler = eventHandler;
        }

        public IEnumerable<EventMessage> Invoke(Func<T, IEnumerable<IEvent>> action, Subscriptions subscriptions)
        {
            var generatedEvents = action(actionHandler);
            return subscriptions.ToMessages(generatedEvents, Outbox);
        }
    }
}