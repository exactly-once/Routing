using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterInstance
    {
        [JsonConstructor]
        public RouterInstance(string instanceId, Dictionary<string, string> interfacesToSites)
        {
            InstanceId = instanceId;
            InterfacesToSites = interfacesToSites;
        }

        public string InstanceId { get; }
        /// <summary>
        /// Maps site names to input queue names.
        /// </summary>
        public Dictionary<string, string> InterfacesToSites { get; }
    }
}