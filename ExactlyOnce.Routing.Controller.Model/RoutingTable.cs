using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RoutingTable : IEventHandler<MessageRoutingChanged>,
        IEventHandler<RouteChanged>,
        IEventHandler<DestinationSiteToNextHopMapChanged>,
        IEventHandler<EndpointInstanceLocationUpdated>,
        IEventHandler<RouterInstanceUpdated>
    {
        public int Version { get; private set; }
        //Each entry is guaranteed to refer to a different handler type
        public Dictionary<string, List<RoutingTableEntry>> Entries { get; }
        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMapping { get; private set; }
        public Dictionary<string, string> SiteRoutingPolicy { get; }
        public Dictionary<string, string> DistributionPolicy { get; }
        public Dictionary<string, List<EndpointInstanceId>> Sites { get; }
        public Dictionary<string, List<RouterInstanceInfo>> RouterInstances { get; }
        public List<Redirection> Redirections { get; }

        public RoutingTable()
            : this(0, new Dictionary<string, List<RoutingTableEntry>>(),
                new Dictionary<string, Dictionary<string, DestinationSiteInfo>>(),
                new Dictionary<string, string>(), 
                new Dictionary<string, string>(), 
                new Dictionary<string, List<EndpointInstanceId>> (), 
                new List<Redirection>(), 
                new Dictionary<string, List<RouterInstanceInfo>>())
        {
        }

        [JsonConstructor]
        public RoutingTable(int version, Dictionary<string, List<RoutingTableEntry>> entries,
            Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMapping,
            Dictionary<string, string> siteRoutingPolicy,
            Dictionary<string, string> distributionPolicy,
            Dictionary<string, List<EndpointInstanceId>> sites,
            List<Redirection> redirections, 
            Dictionary<string, List<RouterInstanceInfo>> routerInstances)
        {
            Entries = entries;
            DestinationSiteToNextHopMapping = destinationSiteToNextHopMapping;
            SiteRoutingPolicy = siteRoutingPolicy;
            DistributionPolicy = distributionPolicy;
            Version = version;
            Sites = sites;
            Redirections = redirections;
            RouterInstances = routerInstances;
        }

        public IEnumerable<IEvent> ConfigureSiteRouting(string endpoint, string policyName)
        {
            if (policyName != null)
            {
                SiteRoutingPolicy[endpoint] = policyName;
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
                entry.UpdateSiteRoutingPolicy(policyName);
            }
            return GenerateChangeEvent();
        }

        public IEnumerable<IEvent> ConfigureDistribution(string endpoint, string policyName)
        {
            if (policyName != null)
            {
                DistributionPolicy[endpoint] = policyName;
            }
            else
            {
                DistributionPolicy.Remove(endpoint);
            }

            var existingEntries = Entries.Values
                .SelectMany(x => x)
                .Where(e => e.Endpoint == endpoint);

            foreach (var entry in existingEntries)
            {
                entry.UpdateDistributionPolicy(policyName);
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

            SiteRoutingPolicy.TryGetValue(endpoint, out var routingPolicy);
            DistributionPolicy.TryGetValue(endpoint, out var distributionPolicy);
            routes.Add(new RoutingTableEntry(handlerType, endpoint, sites, routingPolicy, distributionPolicy));

            //When a route is added redirection is removed (if existed)
            Redirections.RemoveAll(x => x.FromHandler == handlerType && x.FromEndpoint == endpoint);
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
            yield return new RoutingTableChanged(Version, Entries, DestinationSiteToNextHopMapping, DistributionPolicy, Sites, Redirections, RouterInstances);
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

            var existingInstanceId = site.FirstOrDefault(x => x.InstanceId == e.InstanceId && x.EndpointName == e.Endpoint);

            if (existingInstanceId == null)
            {
                site.Add(new EndpointInstanceId(e.Endpoint, e.InstanceId, e.InputQueue));
            }
            else
            {
                existingInstanceId.UpdateInputQueue(e.InputQueue);
            }

            if (!DestinationSiteToNextHopMapping.ContainsKey(e.Site))
            {
                var destinationSiteInfos = new Dictionary<string, DestinationSiteInfo>
                {
                    [e.Site] = new DestinationSiteInfo(null, null, 0)
                };
                DestinationSiteToNextHopMapping[e.Site] = destinationSiteInfos;
            }

            return GenerateChangeEvent();
        }

        public IEnumerable<IEvent> Handle(RouterInstanceUpdated e)
        {
            if (!RouterInstances.TryGetValue(e.Router, out var instances))
            {
                instances = new List<RouterInstanceInfo>();
                RouterInstances[e.Router] = instances;
            }
            else
            {
                var existingInstance = instances.FirstOrDefault(x => x.InstanceId == e.InstanceId);
                if (existingInstance != null)
                {
                    existingInstance.Update(e.SiteToQueueMap);
                }
                else
                {
                    var instance = new RouterInstanceInfo(e.InstanceId, e.SiteToQueueMap);
                    instances.Add(instance);
                }
            }

            return GenerateChangeEvent();
        }
    }
}