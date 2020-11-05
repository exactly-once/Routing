using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class Subscriptions
    {
        readonly Dictionary<Type, List<Subscription>> subscriptions = new Dictionary<Type, List<Subscription>>();

        public void Subscribe<TState, TEvent>(Func<TEvent, string> selectDestinationCallback) 
            where TEvent : IEvent
        {
            if (!subscriptions.TryGetValue(typeof(TEvent), out var subs))
            {
                subs = new List<Subscription>();
                subscriptions[typeof(TEvent)] = subs;
            }

            subs.Add(new Subscription(typeof(TState), e => selectDestinationCallback((TEvent)e)));
        }

        public IEnumerable<EventMessage> ToMessages(IEnumerable<IEvent> events, State sourceEntity)
        {
            var source = $"{sourceEntity.GetType().Name}-{sourceEntity.Id}";
            return events.SelectMany(e => ToMessages(e, source, sourceEntity.Outbox));
        }

        IEnumerable<EventMessage> ToMessages(IEvent e, string source, Outbox outbox)
        {
            if (!subscriptions.TryGetValue(e.GetType(), out var subs))
            {
                return Enumerable.Empty<EventMessage>();
                //TODO Uncomment this line when we have all subscription set up
                //throw new Exception($"No function subscribes to {e.GetType()}");
            }

            return subs.Select(sub =>
            {
                var destinationId = sub.SelectDestinationCallback(e);
                var destination = $"{sub.EntityType.Name}-{destinationId}";

                long? sequence;
                string uniqueId;
                if (source != null)
                {
                    sequence = outbox.Stamp(destination);
                    uniqueId = $"{source}-{sequence}";
                }
                else
                {
                    sequence = null;
                    uniqueId = Guid.NewGuid().ToString(); //no deduplication possible
                }

                return new EventMessage(uniqueId, source, sequence, destinationId, sub.EntityType.FullName, e);
            });
        }

        class Subscription
        {
            public Subscription(Type entityType, Func<object, string> selectDestinationCallback)
            {
                EntityType = entityType;
                SelectDestinationCallback = selectDestinationCallback;
            }

            public Type EntityType { get; }
            public Func<object, string> SelectDestinationCallback { get; }
        }
    }
}