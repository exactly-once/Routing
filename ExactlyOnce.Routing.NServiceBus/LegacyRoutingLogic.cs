using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ExactlyOnce.Routing.Client;
using NServiceBus;
using NServiceBus.Pipeline;
using NServiceBus.Routing;

namespace ExactlyOnce.Routing.NServiceBus
{
    public class LegacyRoutingLogic
    {
        readonly RoutingControllerClient routingControllerClient;
        readonly string endpointName;
        readonly Dictionary<string, string> destinations;
        readonly IDistributionPolicy distributionPolicy;
        readonly EndpointInstances endpointInstances;
        readonly Func<EndpointInstance, string> transportAddressTranslation;
        readonly ConcurrentDictionary<string, bool> registeredLegacyDestinations = new ConcurrentDictionary<string, bool>();
        string site;

        public LegacyRoutingLogic(RoutingControllerClient routingControllerClient,
            string endpointName,
            Dictionary<string, string> destinations,
            IDistributionPolicy distributionPolicy,
            EndpointInstances endpointInstances,
            Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.routingControllerClient = routingControllerClient;
            this.endpointName = endpointName;
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

            if (!registeredLegacyDestinations.TryGetValue(messageTypeFullName, out _) && site != null) //site can be null if we are sending before the endpoint has started
            {
                routingControllerClient.RegisterLegacyDestination(
                    endpointName,
                    messageTypeFullName,
                    destinationEndpoint,
                    destinationQueue,
                    site,
                    Guid.NewGuid().ToString()
                );

                registeredLegacyDestinations.AddOrUpdate(messageTypeFullName, true, (key, value) => true);
            }

            return new List<RoutingStrategy>
            {
                new UnicastRoutingStrategy(destinationQueue)
            };
        }

        public void SetSite(string thisSite)
        {
            site = thisSite;
        }
    }
}