using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace ExactlyOnce.Routing.AzureController
{
    public class EndpointApi
    {
        [FunctionName(nameof(ProcessEndpointReport))]
        public async Task<IActionResult> ProcessEndpointReport(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] EndpointReportRequest endpointReport,
            [ExactlyOnce(requestId: "{reportId}", stateId: "{endpointName}")] IOnceExecutor<EndpointState, Endpoint> execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector)
        {
            var messages = await execute.Once(x => x.OnEndpointStartup(endpointReport.InstanceId,
                endpointReport.RecognizedMessages, endpointReport.MessageHandlers));

            foreach (var eventMessage in messages)
            {
                eventCollector.Add(eventMessage);
            }

            return new OkResult();
        }

        [FunctionName(nameof(ProcessEndpointHello))]
        public async Task<IActionResult> ProcessEndpointHello(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] EndpointHelloRequest helloRequest,
            [ExactlyOnce(requestId: "{reportId}", stateId: "{endpointName}")] IOnceExecutor<EndpointState, Endpoint> execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector)
        {
            var messages = await execute.Once(x => x.OnEndpointHello(helloRequest.InstanceId, helloRequest.Site));

            foreach (var eventMessage in messages)
            {
                eventCollector.Add(eventMessage);
            }

            return new OkResult();
        }

        public class EndpointReportRequest
        {
            public string ReportId { get; set; }
            public string EndpointName { get; set; }
            public string InstanceId { get; set; }
            public List<MessageHandlerInstance> MessageHandlers { get; set; }
            public Dictionary<string, MessageKind> RecognizedMessages { get; set; }
        }

        public class EndpointHelloRequest
        {
            public string ReportId { get; set; }
            public string EndpointName { get; set; }
            public string InstanceId { get; set; }
            public string Site { get; set; }
        }
    }
}