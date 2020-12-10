using System.Collections.Generic;
using ExactlyOnce.Routing.Endpoint.Model;
using NServiceBus.Routing;

namespace ExactlyOnce.Routing.NServiceBus
{
    class RoutingSlipRoutingStrategy : RoutingStrategy
    {
        readonly string routedType;
        public RoutingSlip RoutingSlip { get; }

        public RoutingSlipRoutingStrategy(string routedType, RoutingSlip routingSlip)
        {
            this.routedType = routedType;
            RoutingSlip = routingSlip;
        }

        public override AddressTag Apply(Dictionary<string, string> headers)
        {
            RoutingSlip.ApplyTo(headers);
            headers["ExactlyOnce.Routing.RoutedType"] = routedType;
            return new UnicastAddressTag(RoutingSlip.NextHopQueue);
        }
    }
}