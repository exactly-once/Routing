namespace ExactlyOnce.Routing.ApiContract
{
    public class Destination
    {
        public string EndpointName { get; set; }
        public string HandlerType { get; set; }
        public bool Active { get; set; }
    }
}
