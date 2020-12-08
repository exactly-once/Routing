using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class LegacyDestinationAdded : IEvent
    {
        [JsonConstructor]
        public LegacyDestinationAdded(string messageType, string destination, string addedByEndpoint)
        {
            MessageType = messageType;
            Destination = destination;
            AddedByEndpoint = addedByEndpoint;
        }

        public string MessageType { get; }
        public string Destination { get; }
        public string AddedByEndpoint { get; }
    }
}