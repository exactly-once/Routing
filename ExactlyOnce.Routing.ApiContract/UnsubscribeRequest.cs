namespace ExactlyOnce.Routing.ApiContract
{
    public class UnsubscribeRequest
    {
        public string RequestId { get; set; }
        public string MessageType { get; set; }
        public string Endpoint { get; set; }
        public string HandlerType { get; set; }
    }
}