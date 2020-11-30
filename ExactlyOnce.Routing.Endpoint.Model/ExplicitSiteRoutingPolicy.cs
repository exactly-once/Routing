using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class ExplicitSiteRoutingPolicy : ISiteRoutingPolicy
    {
        Dictionary<string, List<string>> reachableSiteMap;

        public void Initialize(RoutingTable routingTable, RoutingTableEntry entry)
        {
            reachableSiteMap = routingTable.DestinationSiteToNextHopMapping
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                        .Select(x => x.Key).Where(x => entry.Sites.Contains(x)).ToList());
        }

        public string GetDestinationSite(SiteRoutingPolicyContext context)
        {
            if (context.ExplicitDestinationSite == null)
            {
                throw new Exception("Explicit site routing policy requires that site is explicitly specified when sending a message.");
            }

            if (!reachableSiteMap.TryGetValue(context.SendingSite, out var reachableSites))
            {
                throw new Exception($"The routing table does not contain information for requested source site {context.SendingSite}");
            }
            if (!reachableSites.Contains(context.ExplicitDestinationSite))
            {
                throw new Exception($"Selected site {context.ExplicitDestinationSite} is not reachable from sending site {context.SendingSite}");
            }

            return context.ExplicitDestinationSite;
        }
    }
}