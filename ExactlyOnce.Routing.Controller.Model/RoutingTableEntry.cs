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
        public List<string> Sites { get; }
        public EndpointSiteRoutingPolicy SiteRoutingPolicy { get; }
    }
}