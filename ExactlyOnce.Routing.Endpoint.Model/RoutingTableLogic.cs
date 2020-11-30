using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RoutingTableLogic
    {
        readonly RoutingTable routingTable;
        readonly DistributionPolicy distributionPolicy;

        public RoutingTableLogic(
            RoutingTable routingTable,
            Dictionary<string, Func<ISiteRoutingPolicy>> siteRoutingPolicyFactories,
            Dictionary<string, Func<IDistributionPolicy>> distributionPolicyFactories
            )
        {
            this.routingTable = routingTable;
            foreach (var entry in routingTable.Entries.Values.SelectMany(x => x))
            {
                InitializeSiteRoutingPolicy(routingTable, siteRoutingPolicyFactories, entry);
            }
            distributionPolicy = new DistributionPolicy(routingTable, distributionPolicyFactories);
        }

        static void InitializeSiteRoutingPolicy(RoutingTable routingTable, Dictionary<string, Func<ISiteRoutingPolicy>> siteRoutingPolicyFactories,
            RoutingTableEntry entry)
        {
            if (!siteRoutingPolicyFactories.TryGetValue(entry.SiteRoutingPolicy ?? "default", out var policyFactory))
            {
                throw new Exception($"Unsupported site routing policy {entry.SiteRoutingPolicy}");
            }

            var policy = policyFactory();
            policy.Initialize(routingTable, entry);
            entry.SiteRoutingPolicyInstance = policy;
        }


        public List<RoutingSlip> SelectDestinations(string messageType, SiteRoutingPolicyContext context)
        {
            if (!routingTable.Entries.TryGetValue(messageType, out var matchingEntries))
            {
                throw new Exception($"Route not found for message type {messageType}");
            }

            return matchingEntries.Select(entry => CreateRoutingSlip(entry, context)).ToList();
        }

        RoutingSlip CreateRoutingSlip(RoutingTableEntry routingTableEntry, SiteRoutingPolicyContext context)
        {
            if (!routingTable.DestinationSiteToNextHopMapping.TryGetValue(context.SendingSite, out var destinationsFromSource))
            {
                throw new Exception($"The routing table does not contain information for requested source site {context.SendingSite}");
            }

            var destinationSite = routingTableEntry.SiteRoutingPolicyInstance.GetDestinationSite(context);

            string nextHopQueue;
            var distributionPolicyContext = new DistributionPolicyContext(context.SendingSite, context.MessageHeaders);
            if (destinationSite == context.SendingSite)
            {
                var nextHop = routingTableEntry.Endpoint;
                nextHopQueue = distributionPolicy.GetDestinationQueueForEndpoint(nextHop, distributionPolicyContext);
            }
            else
            {
                var nextHop = destinationsFromSource[destinationSite].Router;
                nextHopQueue = distributionPolicy.GetDestinationQueueForRouter(nextHop, distributionPolicyContext);
            }

            return new RoutingSlip(routingTableEntry.Handler, routingTableEntry.Endpoint, destinationSite, nextHopQueue);
        }
        

        public RoutingSlip Reroute(string messageType, string destinationHandler, string destinationEndpoint,
            SiteRoutingPolicyContext context)
        {
            if (!routingTable.Entries.TryGetValue(messageType, out var destinations))
            {
                throw new MoveToDeadLetterQueueException($"Message {messageType} cannot be rerouted to destination because message type is not recognized.");
            }

            var currentDestinationHandler = destinationHandler;
            var currentDestinationEndpoint = destinationEndpoint;

            while (true)
            {
                //Redirections are never circular
                var entry = destinations
                    .FirstOrDefault(x => x.Handler == currentDestinationHandler && x.Endpoint == currentDestinationEndpoint);
                var redirection = routingTable.Redirections
                    .FirstOrDefault(x => x.FromHandler == currentDestinationHandler && x.FromEndpoint == currentDestinationEndpoint);
                if (entry != null)
                {
                    if (destinationHandler == currentDestinationHandler &&
                        destinationEndpoint == currentDestinationEndpoint)
                    {
                        return null;
                    }
                    return CreateRoutingSlip(entry, context);
                }
                if (redirection != null)
                {
                    currentDestinationHandler = redirection.ToHandler;
                    currentDestinationEndpoint = redirection.ToEndpoint;
                }
                else
                {
                    throw new MoveToDeadLetterQueueException($"Message {messageType} cannot be rerouted because there is no active route for handler {destinationHandler} at {destinationEndpoint} and no viable redirection exists.");
                }
            }
        }
    }
}