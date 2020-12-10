using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Client
{
    public class RouterInstanceInfo
    {
        [JsonConstructor]
        public RouterInstanceInfo(string instanceId, Dictionary<string, string> siteToInputQueueMap)
        {
            InstanceId = instanceId;
            SiteToInputQueueMap = siteToInputQueueMap;
        }

        public string InstanceId { get; }
        public Dictionary<string, string> SiteToInputQueueMap { get; }
    }
}