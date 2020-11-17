using System;
using System.Collections.Generic;
using ExactlyOnce.Routing.Endpoint.Model;

namespace ExactlyOnce.Routing.NServiceBus
{
    interface IRoutingTable
    {
        IReadOnlyCollection<RoutingSlip> GetRoutesFor(Type messageType, string explicitDestinationSite);
    }
}