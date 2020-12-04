using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class Inbox
    {
        public Dictionary<string, InboxQueue> EventQueues;

        [JsonConstructor]
        public Inbox(Dictionary<string, InboxQueue> eventQueues)
        {
            EventQueues = eventQueues;
        }

        public Inbox() 
            : this(new Dictionary<string, InboxQueue>())
        {
        }

        public IEnumerable<IEvent> AppendAndProcess(EventMessage eventMessage, Func<IEvent, IEnumerable<IEvent>> processCallback)
        {
            if (!eventMessage.Sequence.HasValue)
            {
                return processCallback(eventMessage.Payload);
            }

            if (!EventQueues.TryGetValue(eventMessage.Source, out var queue))
            {
                queue = new InboxQueue(new List<EventMessage>(), -1);
                EventQueues[eventMessage.Source] = queue;
            }

            return queue.AppendAndProcess(eventMessage, processCallback);
        }
    }
}