using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class InactiveRoutingTableEntry
    {
        [JsonConstructor]
        public InactiveRoutingTableEntry(string messageType, RoutingTableEntry entry)
        {
            MessageType = messageType;
            Entry = entry;
        }

        public string MessageType { get; }
        public RoutingTableEntry Entry { get; }
    }
}