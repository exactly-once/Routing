using System;
using System.Collections.Generic;
using System.Linq;
using ExactlyOnce.AzureFunctions;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class EndpointState : State
    {
        public EndpointState(Endpoint endpoint, Inbox inbox, Outbox outbox)
        {
            Endpoint = endpoint;
            Inbox = inbox;
            Outbox = outbox;
        }

        public Endpoint Endpoint { get; }
        public Inbox Inbox { get; }
        public Outbox Outbox { get; }

        public IEnumerable<Event> OnEndpointHello(string instanceId, string site, Routing routing)
        {
            var events = Endpoint.OnEndpointHello(instanceId, site);
            return routing.ToMessages(events, Endpoint.Name, Outbox);
        }

        public IEnumerable<Event> OnEndpointStartup(string instanceId, Dictionary<string, MessageKind> recognizedMessages,
            List<MessageHandlerInstance> messageHandlers, Routing routing)
        {
            var events = Endpoint.OnEndpointStartup(instanceId, recognizedMessages, messageHandlers);
            return routing.ToMessages(events, Endpoint.Name, Outbox);
        }
    }

    public class Routing
    {
        //One queue can be subscribed to single event type
        Dictionary<string, Type> queueToEventMap = new Dictionary<string, Type>();
        readonly Dictionary<Type, List<string>> subscriptions = new Dictionary<Type, List<string>>();

        public void Subscribe(string queueName, Type eventType)
        {
            queueToEventMap.Add(queueName, eventType);
            if (!subscriptions.TryGetValue(eventType, out var subs))
            {
                subs = new List<string>();
                subscriptions[eventType] = subs;
            }
            subs.Add(queueName);
        }

        public IEnumerable<Event> ToMessages(IEnumerable<IEvent> events, string source, Outbox outbox)
        {
            return events.SelectMany(e => ToMessages(e, source, outbox));
        }

        IEnumerable<Event> ToMessages(IEvent e, string source, Outbox outbox)
        {
            if (!subscriptions.TryGetValue(e.GetType(), out var subs))
            {
                throw new Exception($"No function subscribes to {e.GetType()}");
            }

            return subs.Select(s => outbox.Stamp(source, s, e));
        }
    }
}
