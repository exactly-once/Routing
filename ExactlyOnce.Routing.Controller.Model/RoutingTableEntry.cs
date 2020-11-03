using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RoutingTableEntry
    {
        public RoutingTableEntry(string handler, string endpoint, List<string> sites, EndpointSiteRoutingPolicy siteRoutingPolicy)
        {
            Handler = handler;
            Endpoint = endpoint;
            Sites = sites;
            SiteRoutingPolicy = siteRoutingPolicy;
        }

        public string Endpoint { get; }
        public string Handler { get; }
        public List<string> Sites { get; private set; }
        public EndpointSiteRoutingPolicy SiteRoutingPolicy { get; private set; }

        public void UpdateSites(List<string> sites)
        {
            Sites = sites;
        }

        public void UpdateSiteRoutingPolicy(EndpointSiteRoutingPolicy policy)
        {
            SiteRoutingPolicy = policy;
        }
    }
}