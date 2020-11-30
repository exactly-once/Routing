using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
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
        public Dictionary<string, string> SiteToInputQueueMap { get; private set; }

        public void Update(Dictionary<string, string> siteToQueueMap)
        {
            SiteToInputQueueMap = siteToQueueMap;
        }
    }
}