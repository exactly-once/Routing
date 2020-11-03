using System;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class TopologyState : EventDrivenState
    {
        public TopologyState(Inbox inbox, Outbox outbox, Topology topology) 
            : base(inbox, outbox, topology)
        {
            Topology = topology;
        }

        public Topology Topology { get; }
    }
}