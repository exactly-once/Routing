using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
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
        public List<string> Sites { get; private set; }
        public string SiteRoutingPolicy { get; private set; }
        public string DistributionPolicy { get; private set; }

        public void UpdateSites(List<string> sites)
        {
            Sites = sites;
        }

        public void UpdateSiteRoutingPolicy(string policy)
        {
            SiteRoutingPolicy = policy;
        }

        public void UpdateDistributionPolicy(string policy)
        {
            DistributionPolicy = policy;
        }
    }
}