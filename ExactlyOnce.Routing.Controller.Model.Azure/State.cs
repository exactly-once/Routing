using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public abstract class State
    {
        [JsonProperty("id")] public string Id { get; internal set; }

        [JsonProperty("_transactionId")] public Guid? TxId { get; internal set; }

        public Inbox Inbox { get; set; } = new Inbox();
        public Outbox Outbox { get; set; } = new Outbox();

        public IEnumerable<EventMessage> OnEvent(EventMessage eventMessage, Subscriptions subscriptions)
        {
            var generatedEvents = Inbox.AppendAndProcess(eventMessage, Process);
            return subscriptions.ToMessages(generatedEvents, this);
        }

        protected abstract IEnumerable<IEvent> Process(IEvent e);
    }

    public abstract class State<T> : State
    {
        public T Data { get; set; }

        public IEnumerable<EventMessage> Invoke(
            Func<T, IEnumerable<IEvent>> action, 
            Func<T> constructor,
            Subscriptions subscriptions)
        {
            Data ??= constructor();
            var generatedEvents = action(Data);
            return subscriptions.ToMessages(generatedEvents, this);
        }

        protected override IEnumerable<IEvent> Process(IEvent e)
        {
            var dataType = GetType().BaseType.GetGenericArguments()[0];
            Data ??= (T) Activator.CreateInstance(dataType);
            return HandlerInvoker.Process(e, Data, dataType);
        }
    }
}