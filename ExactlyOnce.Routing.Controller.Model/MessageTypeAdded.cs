using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageTypeAdded : IEvent
    {
        [JsonConstructor]
        public MessageTypeAdded(string fullName, MessageKind kind, string endpoint)
        {
            FullName = fullName;
            Kind = kind;
            Endpoint = endpoint;
        }

        public string Endpoint { get; }
        public string FullName { get;}
        public MessageKind Kind { get; }
    }
}