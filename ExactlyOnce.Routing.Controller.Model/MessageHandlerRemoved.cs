using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageHandlerRemoved : IEvent
    {
        [JsonConstructor]
        public MessageHandlerRemoved(string handlerType, string handledMessageType, string endpoint, string site)
        {
            HandlerType = handlerType;
            HandledMessageType = handledMessageType;
            Endpoint = endpoint;
            Site = site;
        }

        public string HandlerType { get; }
        public string HandledMessageType { get; }
        public string Site { get; }
        public string Endpoint { get; }
    }
}