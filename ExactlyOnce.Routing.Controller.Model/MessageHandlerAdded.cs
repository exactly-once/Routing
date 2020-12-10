using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageHandlerAdded : IEvent
    {
        [JsonConstructor]
        public MessageHandlerAdded(string handlerType, string handledMessageType, MessageKind messageKind,
            string endpoint, string site, bool autoSubscribe)
        {
            HandlerType = handlerType;
            HandledMessageType = handledMessageType;
            Endpoint = endpoint;
            Site = site;
            AutoSubscribe = autoSubscribe;
            MessageKind = messageKind;
        }

        public string HandlerType { get; }
        public string HandledMessageType { get; }
        public MessageKind MessageKind { get; }
        public string Site { get; }
        public bool AutoSubscribe { get; }
        public string Endpoint { get; }
    }
}