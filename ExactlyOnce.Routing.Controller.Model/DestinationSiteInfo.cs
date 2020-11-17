using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class DestinationSiteInfo
    {
        [JsonConstructor]
        public DestinationSiteInfo(string nextHopSite, string router, int cost)
        {
            NextHopSite = nextHopSite;
            Router = router;
            Cost = cost;
        }

        public string NextHopSite { get; }
        public string Router { get; }
        public int Cost { get; }
    }
}