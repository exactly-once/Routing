using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RouteToMostRecentlyAddedSiteRoutingPolicy : ISiteRoutingPolicy
    {
        string endpoint;
        Dictionary<string, string> reachableSiteMap;

        public void Initialize(RoutingTable routingTable, RoutingTableEntry entry)
        {
            endpoint = entry.Endpoint;

            reachableSiteMap = routingTable.DestinationSiteToNextHopMapping
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => entry.Sites
                        .LastOrDefault(x => kvp.Value.ContainsKey(x)));
        }

        public string GetDestinationSite(SiteRoutingPolicyContext context)
        {
            if (context.ExplicitDestinationSite != null)
            {
                throw new Exception("Most-recent site routing policy prevents explicit destination site specification.");
            }
            if (!reachableSiteMap.TryGetValue(context.SendingSite, out var nearestSite))
            {
                throw new Exception($"The routing table does not contain information for requested source site {context.SendingSite}");
            }
            if (nearestSite == null)
            {
                throw new Exception($"No instance of endpoint {endpoint} is reachable from site {context.SendingSite}.");
            }
            return nearestSite;
        }
    }
}