using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.Pipeline;
using NServiceBus.Routing;

namespace ExactlyOnce.Routing.NServiceBus
{
    public class LegacyRoutingLogic
    {
        readonly Dictionary<string, string> destinations;
        readonly IDistributionPolicy distributionPolicy;
        readonly EndpointInstances endpointInstances;
        readonly Func<EndpointInstance, string> transportAddressTranslation;

        public LegacyRoutingLogic(
            Dictionary<string, string> destinations,
            IDistributionPolicy distributionPolicy,
            EndpointInstances endpointInstances,
            Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.destinations = destinations;
            this.distributionPolicy = distributionPolicy;
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public List<RoutingStrategy> Route(IOutgoingSendContext outgoingSendContext)
        {
            var messageTypeFullName = outgoingSendContext.Message.MessageType.FullName;
            // ReSharper disable once AssignNullToNotNullAttribute
            if (!destinations.TryGetValue(messageTypeFullName, out var destinationEndpoint))
            {
                return new List<RoutingStrategy>();
            }

            var instances = endpointInstances.FindInstances(destinationEndpoint);
            var candidateQueues = instances.Select(i => transportAddressTranslation(i));

            var distributionContext = new DistributionContext(candidateQueues.ToArray(), outgoingSendContext.Message, 
                outgoingSendContext.MessageId, outgoingSendContext.Headers, transportAddressTranslation, outgoingSendContext.Extensions);

            var distributionStrategy =
                distributionPolicy.GetDistributionStrategy(destinationEndpoint, DistributionStrategyScope.Send);

            var destinationQueue = distributionStrategy.SelectDestination(distributionContext);

            return new List<RoutingStrategy>
            {
                new UnicastRoutingStrategy(destinationQueue)
            };
        }
    }
}