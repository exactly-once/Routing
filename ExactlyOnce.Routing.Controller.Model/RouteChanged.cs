using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouteChanged : IEvent
    {
        [JsonConstructor]
        public RouteChanged(string messageType, string handlerType, string endpoint, List<string> sites, string newHandlerType)
        {
            MessageType = messageType;
            HandlerType = handlerType;
            Endpoint = endpoint;
            Sites = sites;
            NewHandlerType = newHandlerType;
        }

        public string MessageType { get; }
        public string HandlerType { get; }
        public string NewHandlerType { get; }
        public string Endpoint { get; }
        public List<string> Sites { get; }
    }
}