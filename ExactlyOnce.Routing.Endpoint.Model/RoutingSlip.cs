using System;
using System.Collections.Generic;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RoutingSlip
    {
        public RoutingSlip(string destinationHandler, string destinationEndpoint,
            string destinationSite, string nextHopQueue)
        {
            DestinationHandler = destinationHandler ?? throw new ArgumentNullException(nameof(destinationHandler));
            DestinationEndpoint = destinationEndpoint ?? throw new ArgumentNullException(nameof(destinationEndpoint));
            DestinationSite = destinationSite;
            NextHopQueue = nextHopQueue;
        }

        public void ApplyTo(Dictionary<string, string> headers)
        {
            headers["ExactlyOnce.Routing.DestinationSite"] = DestinationSite;
            headers["ExactlyOnce.Routing.DestinationEndpoint"] = DestinationEndpoint;
            headers["ExactlyOnce.Routing.DestinationHandler"] = DestinationHandler;
        }

        public string DestinationHandler { get; }
        public string DestinationEndpoint { get; }
        public string NextHopQueue { get; }
        public string DestinationSite { get; }
    }
}