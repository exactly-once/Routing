using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouteRemoved
    {
        [JsonConstructor]
        public RouteRemoved(string messageType, string handlerType, string endpoint)
        {
            MessageType = messageType;
            HandlerType = handlerType;
            Endpoint = endpoint;
        }

        public string MessageType { get; }
        public string HandlerType { get; }
        public string Endpoint { get; }
    }
}