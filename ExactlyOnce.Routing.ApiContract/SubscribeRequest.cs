namespace ExactlyOnce.Routing.ApiContract
{
    public class SubscribeRequest
    {
        public string RequestId { get; set; }
        public string MessageType { get; set; }
        public string Endpoint { get; set; }
        public string HandlerType { get; set; }
        public string ReplacedHandlerType { get; set; }
    }
}