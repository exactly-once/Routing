using System;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RoutingSlip
    {
        public RoutingSlip(string destinationHandler, string destinationEndpoint, string destinationSite, string nextHopSite, string nextHop)
        {
            DestinationHandler = destinationHandler ?? throw new ArgumentNullException(nameof(destinationHandler));
            DestinationEndpoint = destinationEndpoint ?? throw new ArgumentNullException(nameof(destinationEndpoint));
            DestinationSite = destinationSite;
            NextHopSite = nextHopSite;
            NextHop = nextHop;
        }

        public string DestinationHandler { get; }
        public string DestinationEndpoint { get; }
        public string DestinationSite { get; }
        public string NextHopSite { get; }
        public string NextHop { get; }
    }
}