using System;
using System.Collections.Generic;
using System.Linq;
using ExactlyOnce.Routing.Endpoint.Model;
using NServiceBus;
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

        public IEnumerable<RoutingStrategy> Route(Type messageType, IOutgoingContext context, OutgoingLogicalMessage outgoingMessage, DistributionStrategyScope distributionStrategyScope)
        {
            var explicitDestinationSite = GetDestinationSite(context);
            var routes = routingTable.GetRoutesFor(messageType, explicitDestinationSite);

            foreach (var destination in routes)
            {
                var candidates = EndpointNameToTransportAddresses(destination.NextHop).ToArray();
                var distributionContext = new DistributionContext(candidates, outgoingMessage, context.MessageId, context.Headers, resolveTransportAddress, context.Extensions);
                var distributionStrategy = distributionPolicy.GetDistributionStrategy(destination.NextHop, distributionStrategyScope);
                var selected = distributionStrategy.SelectDestination(distributionContext);

                var routingStrategy = new MapBasedRoutingStrategy(selected, explicitDestinationSite, messageType, destination);
                yield return routingStrategy;
            }
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
            readonly Type routedType;
            readonly RoutingSlip routingSlip;

            public MapBasedRoutingStrategy(string destinationQueue, string explicitDestinationSite, Type routedType, RoutingSlip routingSlip)
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
                headers["ExactlyOnce.Routing.RoutedType"] = routedType.FullName;
                return new UnicastAddressTag(destinationQueue);
            }
        }
    }
}