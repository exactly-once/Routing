namespace ExactlyOnce.Routing.ApiContract
{
    public class DestinationInfo
    {
        public string EndpointName { get; set; }
        public string HandlerType { get; set; }
        public bool Active { get; set; }
    }
}
