using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Endpoint = ExactlyOnce.Routing.Controller.Model.Endpoint;

namespace ExactlyOnce.Routing.AzureController
{
    public class EndpointApi
    {
        [FunctionName(nameof(ProcessEndpointReport))]
        public async Task<IActionResult> ProcessEndpointReport(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] EndpointReportRequest request,
            [ExactlyOnce(requestId: "{reportId}", stateId: "{endpointName}")] IOnceExecutor<EndpointState, Endpoint> execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector)
        {
            var messageHandlers = request.MessageHandlers != null 
                ? request.MessageHandlers.Select(kvp => new MessageHandlerInstance(kvp.Key, kvp.Value)).ToList() 
                : new List<MessageHandlerInstance>();
            
            var messageKinds = request.RecognizedMessages ?? new Dictionary<string, MessageKind>();

            var messages = await execute.Once(
                e => e.OnStartup(request.InstanceId, request.InputQueue, messageKinds, messageHandlers, request.AutoSubscribe),
                () => new Endpoint(request.EndpointName));

            foreach (var message in messages)
            {
                eventCollector.Add(message);
            }
            return new OkResult();
        }

        [FunctionName(nameof(ProcessEndpointHello))]
        public async Task<IActionResult> ProcessEndpointHello(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] EndpointHelloRequest helloRequest,
            [ExactlyOnce(requestId: "{reportId}", stateId: "{endpointName}")] IOnceExecutor<EndpointState, Endpoint> execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector)
        {
            var messages = await execute.Once(
                e => e.OnHello(helloRequest.InstanceId, helloRequest.Site),
                () => new Endpoint(helloRequest.EndpointName));

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
            public string InputQueue { get; set; }
            public string InstanceId { get; set; }
            public Dictionary<string, string> MessageHandlers { get; set; }
            public Dictionary<string, MessageKind> RecognizedMessages { get; set; }
            public bool AutoSubscribe { get; set; }
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