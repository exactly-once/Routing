using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RoutingTable : IEventHandler<RouteAdded>,
        IEventHandler<RouteRemoved>,
        IEventHandler<RouteChanged>,
        IEventHandler<DestinationSiteToNextHopMapChanged>
    {
        public int Version { get; private set; }
        //Each entry is guaranteed to refer to a different handler type
        public Dictionary<string, List<RoutingTableEntry>> Entries { get; }
        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMapping { get; private set; }
        public Dictionary<string, EndpointSiteRoutingPolicy> SiteRoutingPolicy { get; }

        //TODO: How to represent replacing one handler with another?

        public RoutingTable()
            : this(
                new Dictionary<string, List<RoutingTableEntry>>(),
                new Dictionary<string, Dictionary<string, DestinationSiteInfo>>(),
                new Dictionary<string, EndpointSiteRoutingPolicy>(), 
                0)
        {
        }

        [JsonConstructor]
        public RoutingTable(
            Dictionary<string, List<RoutingTableEntry>> entries, 
            Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMapping, 
            Dictionary<string, EndpointSiteRoutingPolicy> siteRoutingPolicy, int version)
        {
            Entries = entries;
            DestinationSiteToNextHopMapping = destinationSiteToNextHopMapping;
            SiteRoutingPolicy = siteRoutingPolicy;
            Version = version;
        }

        public IEnumerable<IEvent> ConfigureEndpointSiteRouting(string endpoint, EndpointSiteRoutingPolicy? policy)
        {
            if (policy.HasValue)
            {
                SiteRoutingPolicy[endpoint] = policy.Value;
            }
            else
            {
                SiteRoutingPolicy.Remove(endpoint);
            }

            var existingEntries = Entries.Values
                .SelectMany(x => x)
                .Where(e => e.Endpoint == endpoint);

            foreach (var entry in existingEntries)
            {
                entry.UpdateSiteRoutingPolicy(policy ?? EndpointSiteRoutingPolicy.RouteToOldest);
            }
            return GenerateChangeEvent();
        }

        public IEnumerable<IEvent> OnRouteAdded(string messageType, string handlerType, string endpoint, List<string> sites)
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
            return GenerateChangeEvent();
        }

        public IEnumerable<IEvent> OnRouteChanged(string messageType, string handlerType, string endpoint, List<string> sites)
        {
            var existing = Entries[messageType].Single(e => e.Handler == handlerType && e.Endpoint == endpoint);
            existing.UpdateSites(sites);
            return GenerateChangeEvent();
        }

        public IEnumerable<IEvent> OnRouteRemoved(string messageType, string handlerType, string endpoint)
        {
            var existing = Entries[messageType].Single(e => e.Handler == handlerType && e.Endpoint == endpoint);
            Entries[messageType].Remove(existing);
            return GenerateChangeEvent();
        }

        public IEnumerable<IEvent> OnTopologyChanged(Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMap)
        {
            DestinationSiteToNextHopMapping = destinationSiteToNextHopMap;
            return GenerateChangeEvent();
        }

        IEnumerable<IEvent> GenerateChangeEvent()
        {
            Version++;
            yield return new RoutingTableChanged(Version, Entries, DestinationSiteToNextHopMapping);
        }

        public IEnumerable<IEvent> Handle(RouteAdded e)
        {
            return OnRouteAdded(e.MessageType, e.HandlerType, e.Endpoint, e.Sites);
        }

        public IEnumerable<IEvent> Handle(RouteRemoved e)
        {
            return OnRouteRemoved(e.MessageType, e.HandlerType, e.Endpoint);
        }

        public IEnumerable<IEvent> Handle(RouteChanged e)
        {
            return OnRouteChanged(e.MessageType, e.HandlerType, e.Endpoint, e.Sites);
        }

        public IEnumerable<IEvent> Handle(DestinationSiteToNextHopMapChanged e)
        {
            return OnTopologyChanged(e.DestinationSiteToNextHopMap);
        }
    }
}