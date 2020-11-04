using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class DestinationSiteToNextHopMapChanged : IEvent
    {
        [JsonConstructor]
        public DestinationSiteToNextHopMapChanged(Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMap)
        {
            DestinationSiteToNextHopMap = destinationSiteToNextHopMap;
        }

        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMap { get; }
    }
}