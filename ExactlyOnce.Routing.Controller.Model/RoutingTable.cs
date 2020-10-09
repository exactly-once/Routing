using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RoutingTable
    {
        //Each entry is guaranteed to refer to a different handler type
        public Dictionary<string, List<RoutingTableEntry>> Entries { get; }
        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMapping { get; }
        public Dictionary<string, EndpointSiteRoutingPolicy> SiteRoutingPolicy { get; }

        //TODO: How to represent replacing one handler with another?

        public RoutingTable(
            Dictionary<string, List<RoutingTableEntry>> entries, 
            Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMapping)
        {
            Entries = entries;
            DestinationSiteToNextHopMapping = destinationSiteToNextHopMapping;
        }

        public void ConfigureEndpointSiteMapping(string endpoint, EndpointSiteRoutingPolicy policy)
        {
            SiteRoutingPolicy[endpoint] = policy;
        }

        public void OnRouteAdded(string messageType, string handlerType, string endpoint, List<string> sites)
        {
            if (!Entries.TryGetValue(messageType, out var routes))
            {
                routes = new List<RoutingTableEntry>();
                Entries[messageType] = routes;
            }
            if (!SiteRoutingPolicy.TryGetValue(endpoint, out var policy))
            {
                policy = EndpointSiteRoutingPolicy.RouteToOldest;
            }
            routes.Add(new RoutingTableEntry(handlerType, endpoint, sites, policy));
        }

        public void OnRouteChanged(string messageType, string handlerType, string endpoint, List<string> sites)
        {
            var existing = Entries[messageType].Single(e => e.Handler == handlerType && e.Endpoint == endpoint);

            Entries[messageType].Remove(existing);
            Entries[messageType].Add(new RoutingTableEntry(handlerType, endpoint, sites, existing.SiteRoutingPolicy));
        }

        public void OnRouteRemoved(string messageType, string handlerType, string endpoint)
        {
            var existing = Entries[messageType].Single(e => e.Handler == handlerType && e.Endpoint == endpoint);
            Entries[messageType].Remove(existing);
        }

        public static RoutingTable Derive(RoutingData routingDataInformation, Topology topologyInformation)
        {
            var entries = routingDataInformation.MessageRouting
                .ToDictionary(x => x.Key, x => ToRoutingEntry(x.Value, routingDataInformation));

            return new RoutingTable(entries, topologyInformation.DestinationSiteToNextHopMap);
        }

        static List<RoutingTableEntry> ToRoutingEntry(MessageRouting messageRouting, RoutingData routingDataInformation)
        {
            return messageRouting.Destinations.Select(x =>
            {
                if (!routingDataInformation.EndpointSiteRoutingPolicy.TryGetValue(x.Endpoint, out var policy))
                {
                    policy = SiteRoutingPolicy.RouteToOldest;
                }
                return new RoutingTableEntry(x.Handler, x.Endpoint, x.Sites, policy);
            }).ToList();
        }
    }
}