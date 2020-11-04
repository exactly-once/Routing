using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class TopologyChanged : IEvent
    {
        [JsonConstructor]
        public TopologyChanged(List<string> sites, List<Connection> connections, List<DeniedConnection> deniedConnections)
        {
            Sites = sites;
            Connections = connections;
            DeniedConnections = deniedConnections;
        }

        public List<string> Sites { get; set; }
        public List<Connection> Connections { get; set; }
        public List<DeniedConnection> DeniedConnections { get; set; }
    }
}