namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class DestinationSiteInfo
    {
        public DestinationSiteInfo(string nextHopSite, int cost)
        {
            NextHopSite = nextHopSite;
            Cost = cost;
        }

        public string NextHopSite { get; }
        public int Cost { get; }
    }
}