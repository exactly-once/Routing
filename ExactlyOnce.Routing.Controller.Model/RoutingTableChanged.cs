using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RoutingTableChanged : IEvent
    {
        [JsonConstructor]
        public RoutingTableChanged(int version, Dictionary<string, List<RoutingTableEntry>> entries,
            Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMapping,
            Dictionary<string, string> distributionPolicy,
            Dictionary<string, List<EndpointInstanceId>> sites,
            List<Redirection> redirections, 
            Dictionary<string, List<RouterInstanceInfo>> routerInstances)
        {
            Version = version;
            Entries = entries;
            DestinationSiteToNextHopMapping = destinationSiteToNextHopMapping;
            DistributionPolicy = distributionPolicy;
            Sites = sites;
            Redirections = redirections;
            RouterInstances = routerInstances;
        }

        public int Version { get; }
        public Dictionary<string, List<RoutingTableEntry>> Entries { get; }
        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMapping { get; }
        public Dictionary<string, string> DistributionPolicy { get; }
        public Dictionary<string, List<EndpointInstanceId>> Sites { get; }
        public List<Redirection> Redirections { get; }
        public Dictionary<string, List<RouterInstanceInfo>> RouterInstances { get; }
    }
}