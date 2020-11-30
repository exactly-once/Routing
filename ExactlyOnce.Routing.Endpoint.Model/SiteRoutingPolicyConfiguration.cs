using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class SiteRoutingPolicyConfiguration
    {
        readonly Dictionary<string, Func<ISiteRoutingPolicy>> routingPolicies = new Dictionary<string, Func<ISiteRoutingPolicy>>
        {
            {"default", () => new RouteToMostRecentlyAddedSiteRoutingPolicy()},
            {"explicit", () => new ExplicitSiteRoutingPolicy()},
            {"round-robin", () => new RoundRobinSiteRoutingPolicy()},
            {"most-recently-added", () => new RouteToMostRecentlyAddedSiteRoutingPolicy()},
            {"nearest", () => new RouteToNearestSiteRoutingPolicy()},
        };

        public void AddSiteRoutingPolicy(string name, Func<ISiteRoutingPolicy> policyFactory)
        {
            routingPolicies.Add(name, policyFactory);
        }

        public void InitializeSiteRoutingPolicies(RoutingTable routingTable)
        {
            foreach(var entry in routingTable.Entries.Values.SelectMany(x => x))
            {
                InitializeSiteRoutingPolicy(routingTable, routingPolicies, entry);
            }
        }

        static void InitializeSiteRoutingPolicy(RoutingTable routingTable, Dictionary<string, Func<ISiteRoutingPolicy>> siteRoutingPolicyFactories,
            RoutingTableEntry entry)
        {
            if (!siteRoutingPolicyFactories.TryGetValue(entry.SiteRoutingPolicy ?? "default", out var policyFactory))
            {
                throw new Exception($"Unsupported site routing policy {entry.SiteRoutingPolicy}");
            }

            var policy = policyFactory();
            policy.Initialize(routingTable, entry);
            entry.SiteRoutingPolicyInstance = policy;
        }
    }
}