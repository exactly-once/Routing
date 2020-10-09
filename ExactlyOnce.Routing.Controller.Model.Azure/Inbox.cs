using System;
using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class Inbox
    {
        public Dictionary<string, InboxQueue> EventQueues;

        public Inbox(Dictionary<string, InboxQueue> eventQueues)
        {
            EventQueues = eventQueues;
        }

        public void AppendAndProcess(Event evnt, Action<object> processCallback)
        {
            if (!EventQueues.TryGetValue(evnt.Source, out var queue))
            {
                queue = new InboxQueue(new List<Event>(), -1);
            }

            queue.AppendAndProcess(evnt, processCallback);
        }
    }
}