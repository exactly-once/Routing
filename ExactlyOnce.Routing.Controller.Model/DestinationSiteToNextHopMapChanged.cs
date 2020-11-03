using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class DestinationSiteToNextHopMapChanged : IEvent
    {
        public DestinationSiteToNextHopMapChanged(Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMap)
        {
            DestinationSiteToNextHopMap = destinationSiteToNextHopMap;
        }

        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMap { get; }
    }
}