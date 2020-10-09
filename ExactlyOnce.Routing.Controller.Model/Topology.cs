using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class Topology
    {
        public Topology(Dictionary<string, RouterInfo> routers, List<RouterInterfaceRule> rules, Dictionary<string, Dictionary<string, DestinationSiteInfo>> destinationSiteToNextHopMap)
        {
            Routers = routers;
            Rules = rules;
            DestinationSiteToNextHopMap = destinationSiteToNextHopMap;
        }

        //Used for building routing table
        public Dictionary<string, Dictionary<string, DestinationSiteInfo>> DestinationSiteToNextHopMap { get; private set; }

        //Source information
        public Dictionary<string, RouterInfo> Routers { get; }
        public List<RouterInterfaceRule> Rules { get; }


        /*
         * Problems
         *  - How to route commands and events if multiple same-price routes exist i.e. there are instances of handler in two places
         *  that are the same distance away
         *  - Sometimes we want round-robin if we are using multiple sites to handle larger traffic
         *  - Sometimes we want to be explicit e.g. when destination endpoints are in separate physical locations
         *  - Sometimes we want to inspect the message headers
         *
         *
         *  Router/interface list -> connection list -> active connection list -> 
         */

        public TopologyInfo UpdateRouter(string routerName, RouterPendingChanges changeSet)
        {
            if (!Routers.TryGetValue(routerName, out var router))
            {
                router = new RouterInfo(new List<string>());
                Routers[routerName] = router;
            }

            router.InterfacesAdded(changeSet.InterfacesAdded);
            router.InterfacesRemoved(changeSet.InterfacesRemoved);

            return RebuildDataStructures();
        }

        TopologyInfo RebuildDataStructures()
        {
            var allowedConnections = new List<Connection>();
            var deniedConnections = new List<DeniedConnection>();
            var connectionCandidates = Routers.SelectMany(GenerateConnections).ToArray();

            foreach (var candidate in connectionCandidates)
            {
                var evaluationResult = Evaluate(candidate);
                if (evaluationResult.HasValue)
                {
                    deniedConnections.Add(new DeniedConnection(candidate.SourceSite, candidate.DestinationSite,
                        candidate.Router, evaluationResult.Value));
                }
                else
                {
                    allowedConnections.Add(candidate);
                }
            }

            var sites = Routers.Values.SelectMany(r => r.Interfaces).Distinct().ToList();
            DestinationSiteToNextHopMap = BuildDestinationSiteToNextHopMap(sites, allowedConnections);
            return new TopologyInfo(sites, allowedConnections, deniedConnections);

        }

        /// <summary>
        /// Returns null if connection is allowed or rule index x if connection is denied by rule x
        /// </summary>
        int? Evaluate(Connection connectionCandidate)
        {
            var result = Rules
                .Select((r, i) => new { i, value = r.Evaluate(connectionCandidate)})
                .FirstOrDefault(x => x.value.HasValue);

            if (result == null)
            {
                return null;
            }

            if (result.value == RouterInterfaceRuleAction.Allow)
            {
                return null;
            }

            return result.i;
        }

        static IEnumerable<Connection> GenerateConnections(KeyValuePair<string, RouterInfo> routerInfo)
        {
            var allPairs = 
                from source in routerInfo.Value.Interfaces
                from destination in routerInfo.Value.Interfaces
                select new Connection(source, destination, routerInfo.Key);

            return allPairs.Where(p => p.SourceSite != p.DestinationSite);
        }

        static Dictionary<string, Dictionary<string, DestinationSiteInfo>> BuildDestinationSiteToNextHopMap(List<string> sites,
            List<Connection> allowedConnections)
        {
            return sites.ToDictionary(x => x, x => BuildDestinationSiteToNextHopMap(sites, allowedConnections, x));
        }

        static Dictionary<string, DestinationSiteInfo> BuildDestinationSiteToNextHopMap(List<string> sites,
            List<Connection> allowedConnections, string source)
        {
            var result = sites
                .Where(dest => dest != source)
                .Select(dest => new {dest, info = BuildDestinationSiteToNextHopMap(allowedConnections, source, dest)})
                .Where(x => x.info != null)
                .ToDictionary(x => x.dest, x => x.info);

            //Add zero cost route to itself
            result[source] = new DestinationSiteInfo(null, 0);
            return result;
        }

        static DestinationSiteInfo BuildDestinationSiteToNextHopMap(List<Connection> allowedConnections, string source,
            string dest)
        {
            var bestConnection = allowedConnections
                .Where(c => c.SourceSite == source)
                .Select(c => new {c.DestinationSite, cost = CanReach(allowedConnections, dest, c.DestinationSite)})
                .Where(x => x.cost.HasValue)
                .OrderBy(x => x.cost.Value)
                .Select(x => new DestinationSiteInfo(x.DestinationSite, x.cost.Value))
                .FirstOrDefault();

            //Can be null
            return bestConnection;
        }

        static int? CanReach(List<Connection> allowedConnections, string dest, string via, int cost = 1,
            VisitedSite visited = null)
        {
            if (dest == via)
            {
                return cost;
            }

            if (visited != null && visited.HasBeenVisited(via))
            {
                //We are back to the same node without reaching destination
                return null;
            }

            var indirectConnections = allowedConnections.Where(c => c.SourceSite == via);

            return indirectConnections
                .Select(c => CanReach(allowedConnections, dest, c.DestinationSite, cost + 1, new VisitedSite(via, visited)))
                .Min();
        }

        public void AddRule(string router, string sourcePattern, string destinationPattern, RouterInterfaceRuleAction action)
        {

        }

        public void RemoveRule(int ruleIndex)
        {

        }
    }

    public class VisitedSite
    {
        public VisitedSite(string site, VisitedSite previous)
        {
            Site = site;
            Previous = previous;
        }

        public string Site { get; }
        public VisitedSite Previous { get; }

        public bool HasBeenVisited(string candidate)
        {
            return Site == candidate 
                   || (Previous != null && Previous.HasBeenVisited(candidate));
        }
    }

    public class RouterInfo
    {
        public RouterInfo(List<string> interfaces)
        {
            Interfaces = interfaces;
        }

        public List<string> Interfaces { get; }

        public void InterfacesAdded(List<string> interfacesAdded)
        {
            foreach (var addedInterface in interfacesAdded)
            {
                if (!Interfaces.Contains(addedInterface))
                {
                    Interfaces.Add(addedInterface);
                }
            }
        }

        public void InterfacesRemoved(List<string> interfacesRemoved)
        {
            foreach (var removedInterface in interfacesRemoved)
            {
                Interfaces.Remove(removedInterface);
            }
        }
    }

    public class Connection //Graph edge
    {
        public Connection(string sourceSite, string destinationSite, string router)
        {
            SourceSite = sourceSite;
            DestinationSite = destinationSite;
            Router = router;
        }

        public string SourceSite { get; }
        public string DestinationSite { get; }
        public string Router { get; }
    }

    public class DeniedConnection
    {
        public DeniedConnection(string sourceSite, string destinationSite, string router, int deniedByRule)
        {
            SourceSite = sourceSite;
            DestinationSite = destinationSite;
            Router = router;
            DeniedByRule = deniedByRule;
        }

        public string SourceSite { get; }
        public string DestinationSite { get; }
        public string Router { get; }
        public int DeniedByRule { get; set; }
    }
}