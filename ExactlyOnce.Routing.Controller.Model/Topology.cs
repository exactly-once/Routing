using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class Topology : IEventHandler<RouterInterfacesChanged>, IEventHandler<RouterAdded>
    {
        [JsonConstructor]
        public Topology(Dictionary<string, RouterInfo> routers, List<RouterInterfaceRule> rules)
        {
            Routers = routers;
            Rules = rules;
        }

        public Dictionary<string, RouterInfo> Routers { get; }
        public List<RouterInterfaceRule> Rules { get; }

        public IEnumerable<IEvent> Handle(RouterInterfacesChanged e)
        {
            return UpdateRouter(e.Router, e.Interfaces);
        }

        public IEnumerable<IEvent> Handle(RouterAdded e)
        {
            return UpdateRouter(e.Router, e.Interfaces);
        }

        public IEnumerable<IEvent> UpdateRouter(string routerName, List<string> interfaces)
        {
            if (!Routers.TryGetValue(routerName, out var router))
            {
                router = new RouterInfo(interfaces);
                Routers[routerName] = router;
            }
            else
            {
                router.UpdateInterfaces(interfaces);
            }
            return RebuildDataStructures();
        }

        IEnumerable<IEvent> RebuildDataStructures()
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
            var destinationSiteToNextHopMap = BuildDestinationSiteToNextHopMap(sites, allowedConnections);

            yield return new DestinationSiteToNextHopMapChanged(destinationSiteToNextHopMap);
            yield return new TopologyChanged(sites, allowedConnections, deniedConnections);
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
            result[source] = new DestinationSiteInfo(null, null, 0);
            return result;
        }

        static DestinationSiteInfo BuildDestinationSiteToNextHopMap(List<Connection> allowedConnections, string sourceSite,
            string destinationSite)
        {
            var bestConnection = allowedConnections
                .Where(c => c.SourceSite == sourceSite)
                .Select(c => new {c.DestinationSite, c.Router, cost = CanReach(allowedConnections, destinationSite, c.DestinationSite)})
                .Where(x => x.cost.HasValue)
                .OrderBy(x => x.cost.Value)
                .Select(x => new DestinationSiteInfo(x.DestinationSite, x.Router, x.cost.Value))
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
}