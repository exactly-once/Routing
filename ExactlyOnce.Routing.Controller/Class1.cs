using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExactlyOnce.Routing.Controller
{
    public interface IRoutingController
    {
        Task ProcessEndpointInstanceReport(EndpointInstanceReport report);
        Task ProcessRouterReport(RouterReport report);
        Task CommissionRoute(RouteDescription route);
        Task DecommissionRoute(RouteDescription route);
        Task<TopologyDescription> GetCurrentTopology();
        Task<StatusDescription> GetStatus();
    }

    public class StatusDescription
    {
    }

    public class RouteDescription
    {
        //Routes
        //Router connections
    }

    public class TopologyDescription
    {

    }

    public class RouterReport
    {

    }

    public class EndpointInstanceReport
    {
        public string EndpointName { get; set; }
    }
}
