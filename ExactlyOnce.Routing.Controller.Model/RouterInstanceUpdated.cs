using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterInstanceUpdated : IEvent
    {
        [JsonConstructor]
        public RouterInstanceUpdated(string router, string instanceId, Dictionary<string, string> siteToQueueMap)
        {
            Router = router;
            InstanceId = instanceId;
            SiteToQueueMap = siteToQueueMap;
        }

        public string Router { get; }
        public string InstanceId { get; }
        public Dictionary<string, string> SiteToQueueMap { get; }
    }
}