using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RoutingTableEntry
    {
        [JsonConstructor]
        public RoutingTableEntry(string handler, string endpoint, List<string> sites, string siteRoutingPolicy, string distributionPolicy)
        {
            Handler = handler;
            Endpoint = endpoint;
            Sites = sites;
            SiteRoutingPolicy = siteRoutingPolicy;
            DistributionPolicy = distributionPolicy;
        }

        public string Endpoint { get; }
        public string Handler { get; }
        public List<string> Sites { get; }
        public string SiteRoutingPolicy { get; }
        public string DistributionPolicy { get; }

        [JsonIgnore]
        public ISiteRoutingPolicy SiteRoutingPolicyInstance { get; set; }
    }
}