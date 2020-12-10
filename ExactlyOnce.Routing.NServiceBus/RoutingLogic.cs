using System;
using System.Collections.Generic;
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

            return new RoutingSlipRoutingStrategy(messageType, newRoutingSlip);
        }

        public IEnumerable<RoutingSlipRoutingStrategy> Route(Type messageType, IOutgoingContext context)
        {
            var explicitDestinationSite = GetDestinationSite(context);
            var routes = routingTable.GetRoutesFor(messageType, explicitDestinationSite, context.Headers);

            foreach (var destination in routes)
            {
                yield return new RoutingSlipRoutingStrategy(messageType.FullName, destination);
            }
        }

        static string GetDestinationSite(IOutgoingContext context)
        {
            return context.Extensions.TryGet<ExplicitSite>(out var siteSpec) 
                ? siteSpec.Site 
                : null;
        }
    }
}