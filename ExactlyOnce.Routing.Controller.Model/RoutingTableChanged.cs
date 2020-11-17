using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RoutingTableChanged : IEvent
    {
        [JsonConstructor]
        public RoutingTableChanged(int version, Dictionary<string, List<RoutingTableEntry>> entries, Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMapping, Dictionary<string, List<EndpointInstanceId>> sites)
        {
            Version = version;
            Entries = entries;
            DestinationSiteToNextHopMapping = destinationSiteToNextHopMapping;
            Sites = sites;
        }

        public int Version { get; }
        public Dictionary<string, List<RoutingTableEntry>> Entries { get; }
        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMapping { get; }
        public Dictionary<string, List<EndpointInstanceId>> Sites { get; }
    }
}