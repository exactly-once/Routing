namespace ExactlyOnce.Routing.Client
{
    public class LegacyDestinationRequest
    {
        public string RequestId { get; set; }
        public string Site { get; set; }
        public string MessageType { get; set; }
        public string SendingEndpointName { get; set; }
        public string DestinationEndpointName { get; set; }
        public string DestinationQueue { get; set; }
    }
}