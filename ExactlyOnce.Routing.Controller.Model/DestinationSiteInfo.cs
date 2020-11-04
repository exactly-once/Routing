using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class DestinationSiteInfo
    {
        [JsonConstructor]
        public DestinationSiteInfo(string nextHopSite, int cost)
        {
            NextHopSite = nextHopSite;
            Cost = cost;
        }

        public string NextHopSite { get; }
        public int Cost { get; }
    }
}