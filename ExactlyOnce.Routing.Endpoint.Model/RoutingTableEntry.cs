using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RoutingTableEntry
    {
        [JsonConstructor]
        public RoutingTableEntry(string handler, string endpoint, List<string> sites, EndpointSiteRoutingPolicy siteRoutingPolicy)
        {
            Handler = handler;
            Endpoint = endpoint;
            Sites = sites;
            SiteRoutingPolicy = siteRoutingPolicy;
        }

        public string Endpoint { get; }
        public string Handler { get; }
        public List<string> Sites { get; }
        public EndpointSiteRoutingPolicy SiteRoutingPolicy { get; }
    }
}