using System;
using System.Threading.Tasks;
using ExactlyOnce.Routing.ApiContract;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace ExactlyOnce.Routing.AzureController
{
    public class RoutingTableApi
    {
        [FunctionName(nameof(ConfigureEndpointSiteRouting))]
        public async Task<IActionResult> ConfigureEndpointSiteRouting(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] ConfigureEndpointSiteRoutingRequest request,
            [ExactlyOnce(requestId: "{requestId}", stateId: "Instance")] IOnceExecutor<RoutingTableState, RoutingTable> execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector,
            [Queue("signalr")] ICollector<EventMessage> signalrCollector,
            [Queue("routing-table-store")] ICollector<EventMessage> routeStoreCollector)
        {
            var sideEffects = await execute.Once(
                r => r.ConfigureSiteRouting(request.EndpointName, request.Policy),
                () => throw new Exception("Routing table not yet created"));

            foreach (var message in sideEffects)
            {
                if (message.DestinationType == typeof(NotificationApi).FullName) //HACK
                {
                    signalrCollector.Add(message);
                }
                else if (message.DestinationType == typeof(RouteTableStorageApi).FullName) //HACK
                {
                    routeStoreCollector.Add(message);
                }
                else
                {
                    eventCollector.Add(message);
                }
            }
            return new OkResult();
        }
    }
}