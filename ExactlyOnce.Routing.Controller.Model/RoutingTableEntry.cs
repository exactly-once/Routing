using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
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
        public string Handler { get; private set; }
        public List<string> Sites { get; private set; }
        public string SiteRoutingPolicy { get; private set; }
        public string DistributionPolicy { get; private set; }
        public bool Active { get; private set; }

        public void Activate()
        {
            Active = true;
        }

        public void Update(List<string> sites, string newHandlerType)
        {
            Sites = sites;
            Handler = newHandlerType;
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