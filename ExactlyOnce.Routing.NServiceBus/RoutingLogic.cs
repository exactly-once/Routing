using System;
using System.Collections.Generic;
using ExactlyOnce.Routing.Endpoint.Model;
using NServiceBus.Pipeline;
using NServiceBus.Routing;

namespace ExactlyOnce.Routing.NServiceBus
{
    class RoutingLogic
    {
        readonly IRoutingTable routingTable;

        public RoutingLogic(IRoutingTable routingTable)
        {
            this.routingTable = routingTable;
        }

        public RoutingStrategy CheckIfReroutingIsNeeded(IIncomingPhysicalMessageContext context)
        {
            if (context.Message.Headers.ContainsKey("ExactlyOnce.Routing.DisableRerouting"))
            {
                return null;
            }

            if (!context.Message.Headers.TryGetValue("ExactlyOnce.Routing.RoutedType", out var messageType)
                || !context.Message.Headers.TryGetValue("ExactlyOnce.Routing.DestinationHandler", out var destinationHandler)
                || !context.Message.Headers.TryGetValue("ExactlyOnce.Routing.DestinationEndpoint", out var destinationEndpoint)
                || !context.Message.Headers.TryGetValue("ExactlyOnce.Routing.DestinationSite", out var destinationSite))
            {
                //We don't have rerouting information.
                return null;
            }

            var newRoutingSlip = routingTable.CheckIfReroutingIsNeeded(messageType, destinationHandler, destinationEndpoint, destinationSite, context.MessageHeaders);
            if (newRoutingSlip == null)
            {
                //No need to re-route
                return null;
            }

            return new MapBasedRoutingStrategy(messageType, newRoutingSlip);
        }

        public IEnumerable<RoutingStrategy> Route(Type messageType, IOutgoingContext context)
        {
            var explicitDestinationSite = GetDestinationSite(context);
            var routes = routingTable.GetRoutesFor(messageType, explicitDestinationSite, context.Headers);

            foreach (var destination in routes)
            {
                yield return new MapBasedRoutingStrategy(messageType.FullName, destination);
            }
        }

        static string GetDestinationSite(IOutgoingContext context)
        {
            return context.Extensions.TryGet<ExplicitSite>(out var siteSpec) 
                ? siteSpec.Site 
                : null;
        }

        class MapBasedRoutingStrategy : RoutingStrategy
        {
            readonly string routedType;
            readonly RoutingSlip routingSlip;

            public MapBasedRoutingStrategy(string routedType, RoutingSlip routingSlip)
            {
                this.routedType = routedType;
                this.routingSlip = routingSlip;
            }

            public override AddressTag Apply(Dictionary<string, string> headers)
            {
                routingSlip.ApplyTo(headers);
                headers["ExactlyOnce.Routing.RoutedType"] = routedType;
                return new UnicastAddressTag(routingSlip.NextHopQueue);
            }
        }
    }
}