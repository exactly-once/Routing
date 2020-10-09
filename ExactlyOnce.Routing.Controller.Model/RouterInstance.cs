using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterInstance
    {
        public RouterInstance(string instanceId, List<string> interfacesToSites)
        {
            InstanceId = instanceId;
            InterfacesToSites = interfacesToSites;
        }

        public string InstanceId { get; }
        public List<string> InterfacesToSites { get; }
    }
}