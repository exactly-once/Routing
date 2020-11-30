using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RoutingTable
    {
        //Each entry is guaranteed to refer to a different handler type
        public Dictionary<string, List<RoutingTableEntry>> Entries { get; }
        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMapping { get; }
        public Dictionary<string, List<EndpointInstanceId>> Sites { get; }
        public List<Redirection> Redirections { get; }
        public Dictionary<string, string> DistributionPolicy { get; }
        public Dictionary<string, List<RouterInstanceInfo>> RouterInstances { get; }
        public int Version { get; }

        [JsonConstructor]
        public RoutingTable(int version,
            Dictionary<string, List<RoutingTableEntry>> entries,
            Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMapping,
            Dictionary<string, string> distributionPolicy,
            Dictionary<string, List<EndpointInstanceId>> sites,
            List<Redirection> redirections, 
            Dictionary<string, List<RouterInstanceInfo>> routerInstances)
        {
            Entries = entries;
            DestinationSiteToNextHopMapping = destinationSiteToNextHopMapping;
            Sites = sites;
            Redirections = redirections;
            RouterInstances = routerInstances;
            DistributionPolicy = distributionPolicy;
            Version = version;
        }
    }
}