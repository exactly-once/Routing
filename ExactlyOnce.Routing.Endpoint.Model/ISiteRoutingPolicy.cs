namespace ExactlyOnce.Routing.Endpoint.Model
{
    public interface ISiteRoutingPolicy
    {
        void Initialize(RoutingTable routingTable, RoutingTableEntry entry);
        string GetDestinationSite(SiteRoutingPolicyContext context);
    }
}