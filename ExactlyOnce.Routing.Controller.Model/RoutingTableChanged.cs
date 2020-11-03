using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RoutingTableChanged : IEvent
    {
        public RoutingTableChanged(int version, Dictionary<string, List<RoutingTableEntry>> entries, Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMapping)
        {
            Version = version;
            Entries = entries;
            DestinationSiteToNextHopMapping = destinationSiteToNextHopMapping;
        }

        public int Version { get; }
        public Dictionary<string, List<RoutingTableEntry>> Entries { get; }
        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMapping { get; }
    }
}