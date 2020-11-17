using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageRoutingChanged : IEvent
    {
        [JsonConstructor]
        public MessageRoutingChanged(List<RouteAdded> addedRoutes, List<RouteRemoved> removedRoutes)
        {
            AddedRoutes = addedRoutes;
            RemovedRoutes = removedRoutes;
        }

        public List<RouteAdded> AddedRoutes { get; }
        public List<RouteRemoved> RemovedRoutes { get; }
    }
}