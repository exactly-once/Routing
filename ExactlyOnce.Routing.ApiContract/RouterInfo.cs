using System.Collections.Generic;

namespace ExactlyOnce.Routing.ApiContract
{
    public class RouterInfo
    {
        public string Name { get; set; }
        public List<string> InterfacesToSites { get; set; }
        public Dictionary<string, RouterInstanceInfo> Instances { get; set; }
    }

    public class RouterInstanceInfo
    {
        public string InstanceId { get; set; }
        public Dictionary<string, string> InterfacesToSites { get; set; }
    }
}