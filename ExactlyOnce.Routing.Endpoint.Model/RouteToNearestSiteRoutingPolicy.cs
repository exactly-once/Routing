using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RouteToNearestSiteRoutingPolicy : ISiteRoutingPolicy
    {
        Dictionary<string, string> reachableSiteMap;
        string endpoint;

        public void Initialize(RoutingTable routingTable, RoutingTableEntry entry)
        {
            endpoint = entry.Endpoint;
            reachableSiteMap = routingTable.DestinationSiteToNextHopMapping
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                        .Where(x => entry.Sites.Contains(x.Key))
                        .OrderBy(x => x.Value.Cost)
                        .Select(x => x.Key)
                        .FirstOrDefault());
        }

        public string GetDestinationSite(SiteRoutingPolicyContext context)
        {
            if (context.ExplicitDestinationSite != null)
            {
                throw new Exception("Nearest site routing policy prevents explicit destination site specification.");
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