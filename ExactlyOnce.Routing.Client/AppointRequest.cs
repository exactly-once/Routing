namespace ExactlyOnce.Routing.Client
{
    public class AppointRequest
    {
        public string RequestId { get; set; }
        public string MessageType { get; set; }
        public string Endpoint { get; set; }
        public string HandlerType { get; set; }
    }
}