using System.Collections.Generic;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class DistributionPolicyContext
    {
        public DistributionPolicyContext(string destinationSite, IReadOnlyDictionary<string, string> messageHeaders)
        {
            DestinationSite = destinationSite;
            MessageHeaders = messageHeaders;
        }

        public string DestinationSite { get; }
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }
    }
}