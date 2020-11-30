using System;
using System.Collections.Generic;
using ExactlyOnce.Routing.Endpoint.Model;

namespace ExactlyOnce.Routing.NServiceBus
{
    interface IRoutingTable
    {
        IReadOnlyCollection<RoutingSlip> GetRoutesFor(Type messageType, string explicitDestinationSite, IReadOnlyDictionary<string, string> headers);
        RoutingSlip CheckIfReroutingIsNeeded(string messageType, string destinationHandler, string destinationEndpoint, string explicitDestinationSite, IReadOnlyDictionary<string, string> headers);
    }
}