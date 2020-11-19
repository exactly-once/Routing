using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageRoutingChanged : IEvent
    {
        [JsonConstructor]
        public MessageRoutingChanged(RouteAdded addedRoute, List<RouteRemoved> removedRoutes)
        {
            AddedRoute = addedRoute;
            RemovedRoutes = removedRoutes;
        }

        public MessageRoutingChanged(List<RouteRemoved> removedRoutes)
        {
            RemovedRoutes = removedRoutes;
        }

        public MessageRoutingChanged(RouteAdded addedRoute)
        {
            AddedRoute = addedRoute;
        }

        public RouteAdded AddedRoute { get; }
        public List<RouteRemoved> RemovedRoutes { get; }
    }
}