using System.Collections.Generic;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class SiteRoutingPolicyContext
    {
        public SiteRoutingPolicyContext(string sendingSite, string explicitDestinationSite, IReadOnlyDictionary<string, string> messageHeaders)
        {
            SendingSite = sendingSite;
            ExplicitDestinationSite = explicitDestinationSite;
            MessageHeaders = messageHeaders;
        }

        public string SendingSite { get; }
        public string ExplicitDestinationSite { get; }
        public IReadOnlyDictionary<string, string> MessageHeaders { get; }
    }
}