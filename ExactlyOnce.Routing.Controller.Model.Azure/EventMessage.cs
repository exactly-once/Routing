namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class EventMessage
    {
        public EventMessage(string uniqueId, string source, long? sequence, string destinationId,
            string destinationType, IEvent payload)
        {
            Source = source;
            Sequence = sequence;
            DestinationId = destinationId;
            DestinationType = destinationType;
            Payload = payload;
            UniqueId = uniqueId;
        }

        public string UniqueId { get; }
        public IEvent Payload { get; }
        public long? Sequence { get; }
        public string DestinationId { get; }
        public string DestinationType { get; }
        public string Source { get; }
    }
}