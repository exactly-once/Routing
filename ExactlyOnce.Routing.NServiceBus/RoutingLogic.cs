using System;
using System.Collections.Generic;
using System.Linq;
using ExactlyOnce.Routing.Endpoint.Model;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Pipeline;
using NServiceBus.Routing;

namespace ExactlyOnce.Routing.NServiceBus
{
    class RoutingLogic
    {
        readonly IRoutingTable routingTable;
        readonly EndpointInstances endpointInstances;
        readonly IDistributionPolicy distributionPolicy;
        readonly Func<EndpointInstance, string> resolveTransportAddress;

        public RoutingLogic(
            IRoutingTable routingTable,
            EndpointInstances endpointInstances, 
            IDistributionPolicy distributionPolicy, 
            Func<EndpointInstance, string> resolveTransportAddress)
        {
            this.routingTable = routingTable;
            this.endpointInstances = endpointInstances;
            this.distributionPolicy = distributionPolicy;
            this.resolveTransportAddress = resolveTransportAddress;
        }

        public RoutingStrategy CheckIfReroutingIsNeeded(IIncomingPhysicalMessageContext context)
        {
            if (!context.Message.Headers.TryGetValue("ExactlyOnce.Routing.RoutedType", out var messageType)
                || !context.Message.Headers.TryGetValue("ExactlyOnce.Routing.DestinationHandler", out var destinationHandler)
                || !context.Message.Headers.TryGetValue("ExactlyOnce.Routing.DestinationEndpoint", out var destinationEndpoint))
            {
                //We don't have rerouting information.
                return null;
            }

            context.Message.Headers.TryGetValue("ExactlyOnce.Routing.DestinationSite", out var explicitSite);

            var newRoutingSlip = routingTable.CheckIfReroutingIsNeeded(messageType, destinationHandler, destinationEndpoint, explicitSite);
            if (newRoutingSlip == null)
            {
                //No need to re-route
                return null;
            }

            return CreateRoutingStrategy(messageType, context.MessageId, context.Message.Headers, context.Extensions,
                null, DistributionStrategyScope.Send, newRoutingSlip, explicitSite);
        }

        public IEnumerable<RoutingStrategy> Route(Type messageType, IOutgoingContext context, OutgoingLogicalMessage outgoingMessage, DistributionStrategyScope distributionStrategyScope)
        {
            var explicitDestinationSite = GetDestinationSite(context);
            var routes = routingTable.GetRoutesFor(messageType, explicitDestinationSite);

            foreach (var destination in routes)
            {
                yield return CreateRoutingStrategy(messageType.FullName, context.MessageId, context.Headers, context.Extensions, outgoingMessage, 
                    distributionStrategyScope, destination, explicitDestinationSite);
            }
        }

        RoutingStrategy CreateRoutingStrategy(string messageTypeFullName, string messageId, Dictionary<string, string> headers, ContextBag context,
            OutgoingLogicalMessage outgoingMessage, DistributionStrategyScope distributionStrategyScope,
            RoutingSlip destination, string explicitDestinationSite)
        {
            var candidates = EndpointNameToTransportAddresses(destination.NextHop).ToArray();
            var distributionContext = new DistributionContext(candidates, outgoingMessage, messageId, headers,
                resolveTransportAddress, context);
            var distributionStrategy =
                distributionPolicy.GetDistributionStrategy(destination.NextHop, distributionStrategyScope);
            var selected = distributionStrategy.SelectDestination(distributionContext);

            var routingStrategy = new MapBasedRoutingStrategy(selected, explicitDestinationSite, messageTypeFullName, destination);
            return routingStrategy;
        }

        static string GetDestinationSite(IOutgoingContext context)
        {
            return context.Extensions.TryGet<ExplicitSite>(out var siteSpec) 
                ? siteSpec.Site 
                : null;
        }

        IEnumerable<string> EndpointNameToTransportAddresses(string endpoint)
        {
            foreach (var instance in endpointInstances.FindInstances(endpoint))
            {
                yield return resolveTransportAddress(instance);
            }
        }

        class MapBasedRoutingStrategy : RoutingStrategy
        {
            readonly string destinationQueue;
            readonly string explicitDestinationSite;
            readonly string routedType;
            readonly RoutingSlip routingSlip;

            public MapBasedRoutingStrategy(string destinationQueue, string explicitDestinationSite, string routedType, RoutingSlip routingSlip)
            {
                this.destinationQueue = destinationQueue;
                this.explicitDestinationSite = explicitDestinationSite;
                this.routedType = routedType;
                this.routingSlip = routingSlip;
            }

            public override AddressTag Apply(Dictionary<string, string> headers)
            {
                if (explicitDestinationSite != null)
                {
                    headers["ExactlyOnce.Routing.DestinationSite"] = explicitDestinationSite;
                }

                headers["ExactlyOnce.Routing.DestinationEndpoint"] = routingSlip.DestinationEndpoint;
                headers["ExactlyOnce.Routing.DestinationHandler"] = routingSlip.DestinationHandler;
                headers["ExactlyOnce.Routing.RoutedType"] = routedType;
                return new UnicastAddressTag(destinationQueue);
            }
        }
    }
}