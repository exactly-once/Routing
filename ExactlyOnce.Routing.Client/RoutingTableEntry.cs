using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Client
{
    public class RoutingTableEntry
    {
        [JsonConstructor]
        public RoutingTableEntry(string handler, string endpoint, List<string> sites, string siteRoutingPolicy, string distributionPolicy, bool active)
        {
            Handler = handler;
            Endpoint = endpoint;
            Sites = sites;
            SiteRoutingPolicy = siteRoutingPolicy;
            DistributionPolicy = distributionPolicy;
            Active = active;
        }

        public string Endpoint { get; }
        public string Handler { get; }
        public List<string> Sites { get; }
        public string SiteRoutingPolicy { get; }
        public string DistributionPolicy { get; }
        public bool Active { get; set; }
    }
}