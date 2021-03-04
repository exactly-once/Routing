namespace ExactlyOnce.Routing.ApiContract
{
    public class ConfigureEndpointSiteRoutingRequest
    {
        public string RequestId { get; set; }
        public string EndpointName { get; set; }
        public string Policy { get; set; }
    }
}