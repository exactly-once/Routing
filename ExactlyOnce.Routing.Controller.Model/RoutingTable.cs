using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RoutingTable : IEventHandler<MessageRoutingChanged>,
        IEventHandler<RouteChanged>,
        IEventHandler<DestinationSiteToNextHopMapChanged>,
        IEventHandler<EndpointInstanceLocationUpdated>
    {
        public int Version { get; private set; }
        //Each entry is guaranteed to refer to a different handler type
        public Dictionary<string, List<RoutingTableEntry>> Entries { get; }
        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMapping { get; private set; }
        public Dictionary<string, EndpointSiteRoutingPolicy> SiteRoutingPolicy { get; }
        public Dictionary<string, List<EndpointInstanceId>> Sites { get; }
        public List<Redirection> Redirections { get; }

        public RoutingTable()
            : this(0, new Dictionary<string, List<RoutingTableEntry>>(),
                new Dictionary<string, Dictionary<string, DestinationSiteInfo>>(),
                new Dictionary<string, EndpointSiteRoutingPolicy>(), new Dictionary<string, List<EndpointInstanceId>> (), new List<Redirection>())
        {
        }

        [JsonConstructor]
        public RoutingTable(int version, Dictionary<string, List<RoutingTableEntry>> entries,
            Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMapping,
            Dictionary<string, EndpointSiteRoutingPolicy> siteRoutingPolicy,
            Dictionary<string, List<EndpointInstanceId>> sites,
            List<Redirection> redirections)
        {
            Entries = entries;
            DestinationSiteToNextHopMapping = destinationSiteToNextHopMapping;
            SiteRoutingPolicy = siteRoutingPolicy;
            Version = version;
            Sites = sites;
            Redirections = redirections;
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

        void OnRouteAdded(string messageType, string handlerType, string endpoint, List<string> sites)
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

        void OnRouteRemoved(string messageType, string handlerType, string endpoint, string replacingHandler, string replacingEndpoint)
        {
            var existing = Entries[messageType].Single(e => e.Handler == handlerType && e.Endpoint == endpoint);

            if (replacingHandler != null)
            {
                //There can be only one redirection for a given endpoint/handler pair
                Redirections.RemoveAll(x => x.FromEndpoint == handlerType && x.FromEndpoint == endpoint);
                Redirections.Add(new Redirection(handlerType, endpoint, replacingHandler, replacingEndpoint));
            }

            Entries[messageType].Remove(existing);
        }

        IEnumerable<IEvent> GenerateChangeEvent()
        {
            Version++;
            yield return new RoutingTableChanged(Version, Entries, DestinationSiteToNextHopMapping, Sites, Redirections);
        }

        public IEnumerable<IEvent> Handle(RouteChanged e)
        {
            var existing = Entries[e.MessageType].Single(e1 => e1.Handler == e.HandlerType && e1.Endpoint == e.Endpoint);
            existing.UpdateSites(e.Sites);
            return GenerateChangeEvent();
        }

        public IEnumerable<IEvent> Handle(DestinationSiteToNextHopMapChanged e)
        {
            DestinationSiteToNextHopMapping = e.DestinationSiteToNextHopMap;
            return GenerateChangeEvent();
        }

        public IEnumerable<IEvent> Handle(MessageRoutingChanged e)
        {
            foreach (var removed in e.RemovedRoutes)
            {
                OnRouteRemoved(removed.MessageType, removed.HandlerType, removed.Endpoint, e.AddedRoute?.HandlerType, e.AddedRoute?.Endpoint);
            }
            if (e.AddedRoute != null)
            {
                OnRouteAdded(e.AddedRoute.MessageType, e.AddedRoute.HandlerType, e.AddedRoute.Endpoint, e.AddedRoute.Sites);
            }

            return GenerateChangeEvent();
        }

        public IEnumerable<IEvent> Handle(EndpointInstanceLocationUpdated e)
        {
            if (!Sites.TryGetValue(e.Site, out var site))
            {
                site = new List<EndpointInstanceId>();
                Sites[e.Site] = site;
            }

            if (!site.Any(x => x.InstanceId == e.InstanceId && x.EndpointName == e.Endpoint))
            {
                site.Add(new EndpointInstanceId(e.Endpoint, e.InstanceId));
            }

            if (!DestinationSiteToNextHopMapping.ContainsKey(e.Site))
            {
                var destinationSiteInfos = new Dictionary<string, DestinationSiteInfo>
                {
                    [e.Site] = new DestinationSiteInfo(e.Site, e.Site, 0)
                };
                DestinationSiteToNextHopMapping[e.Site] = destinationSiteInfos;
            }

            return GenerateChangeEvent();
        }
    }
}