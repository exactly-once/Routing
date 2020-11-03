using System;
using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class RoutingTableState : EventDrivenState
    {
        public RoutingTableState()
        {
        }

        public RoutingTableState(RoutingTable routingTable, Inbox inbox, Outbox outbox)
            : base(inbox, outbox, routingTable)
        {
            RoutingTable = routingTable;
        }

        public IEnumerable<EventMessage> ConfigureEndpointSiteRouting(string endpoint, EndpointSiteRoutingPolicy? policy, Subscriptions subscriptions)
        {
            var events = RoutingTable.ConfigureEndpointSiteRouting(endpoint, policy);
            return subscriptions.ToMessages(events, Outbox);
        }

        public RoutingTable RoutingTable { get; set; }
    }
}