using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RoundRobinSiteRoutingPolicy : ISiteRoutingPolicy
    {
        int roundRobinValue;
        Dictionary<string, List<string>> reachableSiteMap;
        string endpoint;

        public void Initialize(RoutingTable routingTable, RoutingTableEntry entry)
        {
            endpoint = entry.Endpoint;
            reachableSiteMap = routingTable.DestinationSiteToNextHopMapping
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                        .Select(x => x.Key).Where(x => entry.Sites.Contains(x)).ToList());
        }

        public string GetDestinationSite(SiteRoutingPolicyContext context)
        {
            if (context.ExplicitDestinationSite != null)
            {
                throw new Exception("Round robin site routing policy prevents explicit destination site specification.");
            }
            if (!reachableSiteMap.TryGetValue(context.SendingSite, out var reachableSites))
            {
                throw new Exception($"The routing table does not contain information for requested source site {context.SendingSite}");
            }
            if (!reachableSites.Any())
            {
                throw new Exception($"No instance of endpoint {endpoint} is reachable from site {context.SendingSite}.");
            }
            var value = Interlocked.Increment(ref roundRobinValue);
            return reachableSites[value % reachableSites.Count];
        }
    }
}