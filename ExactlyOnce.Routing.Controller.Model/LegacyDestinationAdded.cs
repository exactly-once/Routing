using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class LegacyDestinationAdded : IEvent
    {
        [JsonConstructor]
        public LegacyDestinationAdded(string handledMessageType, MessageKind messageKind,
            string endpoint, string site)
        {
            HandledMessageType = handledMessageType;
            Endpoint = endpoint;
            Site = site;
            MessageKind = messageKind;
        }

        public string HandledMessageType { get; }
        public MessageKind MessageKind { get; }
        public string Site { get; }
        public string Endpoint { get; }
    }
}