using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace ExactlyOnce.Routing.AzureController
{
    public class RouterApi
    {
        [FunctionName(nameof(ProcessRouterReport))]
        public async Task<IActionResult> ProcessRouterReport(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] RouterReportRequest routerReport,
            [ExactlyOnce(requestId: "{reportId}", stateId: "{routerName}")] IOnceExecutor<RouterState, Router> execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector)
        {
            var messages = await execute.Once(
                r => r.OnStartup(routerReport.InstanceId, routerReport.SiteInterfaces),
                () => new Router(routerReport.RouterName));

            foreach (var eventMessage in messages)
            {
                eventCollector.Add(eventMessage);
            }

            return new OkResult();
        }

        public class RouterReportRequest
        {
            public string ReportId { get; set; }
            public string RouterName { get; set; }
            public string InstanceId { get; set; }
            public List<string> SiteInterfaces { get; set; }
        }
    }
}