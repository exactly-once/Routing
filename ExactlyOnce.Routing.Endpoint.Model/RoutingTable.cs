using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RoutingTable
    {
        //Each entry is guaranteed to refer to a different handler type
        public Dictionary<string, List<RoutingTableEntry>> Entries { get; }
        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMapping { get; }

        public RoutingTable(
            Dictionary<string, List<RoutingTableEntry>> entries, 
            Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMapping)
        {
            Entries = entries;
            DestinationSiteToNextHopMapping = destinationSiteToNextHopMapping;
        }

        public List<RoutingSlip> SelectDestinations(string sendingSite, string messageType, Dictionary<string, string> headers, RoutingContext context)
        {
            if (!Entries.TryGetValue(messageType, out var matchingEntries))
            {
                throw new Exception($"Route not found for message type {messageType}");
            }

            return matchingEntries.Select(entry => CreateRoutingSlip(sendingSite, headers, entry, context)).ToList();
        }

        RoutingSlip CreateRoutingSlip(string sendingSite, Dictionary<string, string> headers, RoutingTableEntry routingTableEntry, RoutingContext context)
        {
            if (!DestinationSiteToNextHopMapping.TryGetValue(sendingSite, out var destinationsFromSource))
            {
                throw new Exception($"The routing table does not contain information for requested source site {sendingSite}");
            }

            //Destination sites collection includes zero-cost entry for itself
            var reachableSites = routingTableEntry.Sites.Where(x => destinationsFromSource.ContainsKey(x)).ToArray();
            if (reachableSites.Length == 0)
            {
                throw new Exception($"Endpoint {routingTableEntry.Endpoint} hosted in sites {string.Join(", ", routingTableEntry.Sites)} is not reachable from sending site {sendingSite}.");
            }

            string destinationSite;
            if (routingTableEntry.SiteRoutingPolicy == EndpointSiteRoutingPolicy.Explicit)
            {
                if (!headers.TryGetValue("ExactlyOnce.Routing.Site", out var siteHeader))
                {
                    throw new Exception($"Site routing policy for endpoint {routingTableEntry.Endpoint} requires that site is explicitly specified when sending a message.");
                }

                if (!reachableSites.Contains(siteHeader))
                {
                    throw new Exception($"Selected site {siteHeader} is not reachable from sending site {sendingSite}");
                }
                destinationSite = siteHeader;
            }
            else if (routingTableEntry.SiteRoutingPolicy == EndpointSiteRoutingPolicy.RoundRobin)
            {
                var roundRobinValue = context.GetNextRoundRobinValueFor(routingTableEntry.Endpoint);
                destinationSite = reachableSites[roundRobinValue % reachableSites.Length];
            }
            else if (routingTableEntry.SiteRoutingPolicy == EndpointSiteRoutingPolicy.RouteToNearest)
            {
                destinationSite = reachableSites
                    .Select(s => new { dest = s, cost = destinationsFromSource[s].Cost})
                    .OrderBy(x => x.cost)
                    .Select(x => x.dest)
                    .First();
            }
            else if (routingTableEntry.SiteRoutingPolicy == EndpointSiteRoutingPolicy.RouteToOldest)
            {
                //TODO: Verify that oldest site entries are at the beginning of the collection
                destinationSite = reachableSites.First();
            }
            else
            {
                throw new Exception($"Unsupported site routing policy {routingTableEntry.SiteRoutingPolicy}");
            }

            var nextHop = destinationsFromSource[destinationSite].NextHopSite;
            return new RoutingSlip(routingTableEntry.Handler, routingTableEntry.Endpoint, destinationSite, nextHop);
        }

        public RoutingSlip GetNextHop(string incomingSite, RoutingSlip routingSlip)
        {
            //The next hop is calculated each time a message goes through a router
            if (!DestinationSiteToNextHopMapping.TryGetValue(incomingSite, out var destinationsFromIncomingSite))
            {
                //TODO: DLQ the message. It got to a site from which it can't be router to its destination
                throw new Exception($"The routing table does not contain information for incoming site {incomingSite}");
            }

            if (!destinationsFromIncomingSite.TryGetValue(routingSlip.DestinationSite, out var destination))
            {
                //TODO: DLQ the message. It got to a site from which it can't be routed to its destination
                throw new Exception($"Site {routingSlip.DestinationSite} hosting endpoint {routingSlip.DestinationEndpoint} is not reachable from {incomingSite}.");
            }

            return new RoutingSlip(routingSlip.DestinationHandler, routingSlip.DestinationEndpoint, routingSlip.DestinationSite, destination.NextHopSite);
        }

        public RoutingSlip Reroute(string thisSite, RoutingSlip routingSlip, string messageType, Dictionary<string, string> headers, RoutingContext routingContext)
        {
            //A message is re-routed when it arrives at the destination endpoint and it does not contain the handler or when the destination endpoint is no longer present
            //in the destination site?

            if (!Entries.TryGetValue(messageType, out var destinations))
            {
                //TODO: DLQ
                throw new Exception($"Message {messageType} cannot be rerouted to destination because message type is not recognized.");
            }

            var newDestination = destinations.FirstOrDefault(x => x.Handler == routingSlip.DestinationHandler);
            if (newDestination == null)
            {
                //TODO: DLQ
                throw new Exception($"Message {messageType} cannot be rerouted because destination handler {routingSlip.DestinationHandler} is not active.");
            }

            return CreateRoutingSlip(thisSite, headers, newDestination, routingContext);
        }

        /*
         * Routing process (sending endpoint)
         *  - Find all handlers that should receive the message
         *  - For each handler find which endpoint hosts it
         *  - For each endpoint find which site it belongs to
         *  - If it is the same site as sender, send directly
         *  - If it is different site, send to local router
         *
         * Routing process (intermediary)
         *  - Find endpoint which hosts the destination handler
         *  - If that endpoint is in one of the sites connected to this router, send directly
         *  - Otherwise find next hop for the site the endpoint belongs to
         *  - Find router name of the next hop site and send
         * 
         */
    }
}