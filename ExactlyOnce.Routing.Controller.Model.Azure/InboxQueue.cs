using System;
using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class InboxQueue
    {
        public InboxQueue(List<Event> queue, long lastProcessed)
        {
            Queue = queue;
            LastProcessed = lastProcessed;
        }

        public List<Event> Queue { get; }
        public long LastProcessed { get; private set; }

        public void AppendAndProcess(Event evnt, Action<object> processCallback)
        {
            var insertAt = Queue.FindIndex(0, x => x.Sequence >= evnt.Sequence);
            if (insertAt == -1) //not found
            {
                Queue.Add(evnt);
            }
            else
            {
                Queue.Insert(insertAt, evnt);
            }

            while (Queue.Count > 0 && Queue[0].Sequence == LastProcessed + 1)
            {
                processCallback(Queue[0].Payload);
                LastProcessed = Queue[0].Sequence;
                Queue.RemoveAt(0);
            }
        }
    }
}