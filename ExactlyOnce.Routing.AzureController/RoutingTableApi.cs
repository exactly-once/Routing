using System;
using System.Threading.Tasks;
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
            [ExactlyOnce(requestId: "{reportId}", stateId: "{TODO}")] IOnceExecutor<RoutingTableState, RoutingTable> execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector)
        {
            var messages = await execute.Once(
                r => r.ConfigureSiteRouting(request.EndpointName, request.Policy),
                () => throw new Exception("Routing table not yet created"));

            foreach (var eventMessage in messages)
            {
                eventCollector.Add(eventMessage);
            }

            return new OkResult();
        }

        public class ConfigureEndpointSiteRoutingRequest
        {
            public string ReportId { get; set; }
            public string EndpointName { get; set; }
            public string Policy { get; set; }
        }
    }
}