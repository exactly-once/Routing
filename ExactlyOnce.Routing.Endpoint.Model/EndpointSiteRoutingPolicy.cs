namespace ExactlyOnce.Routing.Endpoint.Model
{
    public enum EndpointSiteRoutingPolicy
    {
        RouteToNearest,
        RouteToOldest,
        Explicit,
        RoundRobin
    }
}