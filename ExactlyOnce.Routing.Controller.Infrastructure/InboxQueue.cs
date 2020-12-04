using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class InboxQueue
    {
        [JsonConstructor]
        public InboxQueue(List<EventMessage> queue, long lastProcessed)
        {
            Queue = queue;
            LastProcessed = lastProcessed;
        }

        public List<EventMessage> Queue { get; }
        public long LastProcessed { get; private set; }

        public IEnumerable<IEvent> AppendAndProcess(EventMessage eventMessage, Func<IEvent, IEnumerable<IEvent>> processCallback)
        {
            if (!eventMessage.Sequence.HasValue)
            {
                throw new Exception("Event has no sequence number.");
            }

            var insertAt = Queue.FindIndex(0, x => x.Sequence >= eventMessage.Sequence);
            if (insertAt == -1) //not found
            {
                Queue.Add(eventMessage);
            }
            else
            {
                Queue.Insert(insertAt, eventMessage);
            }

            while (Queue.Count > 0 && Queue[0].Sequence == LastProcessed + 1)
            {
                var result = processCallback(Queue[0].Payload);
                foreach (var e in result)
                {
                    yield return e;
                }
                LastProcessed = Queue[0].Sequence.Value;
                Queue.RemoveAt(0);
            }
        }
    }
}