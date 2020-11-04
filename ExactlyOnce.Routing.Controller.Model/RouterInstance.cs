using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterInstance
    {
        [JsonConstructor]
        public RouterInstance(string instanceId, List<string> interfacesToSites)
        {
            InstanceId = instanceId;
            InterfacesToSites = interfacesToSites;
        }

        public string InstanceId { get; }
        public List<string> InterfacesToSites { get; }
    }
}