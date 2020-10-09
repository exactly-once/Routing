using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using ExactlyOnce.AzureFunctions;
using ExactlyOnce.AzureFunctions.Sample;
using ExactlyOnce.Routing.Controller.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Endpoint = ExactlyOnce.Routing.Controller.Model.Endpoint;

namespace ExactlyOnce.Routing.AzureController
{
    public class RoutingController
    {
        Container applicationState;

        public RoutingController(Container applicationState)
        {
            this.applicationState = applicationState;
        }

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "RoutingController")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        [FunctionName(nameof(ProcessEndpointReport))]
        public async Task<IActionResult> ProcessEndpointReport(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] EndpointReportRequest endpointReport,
            [ExactlyOnce(requestId: "{reportId}", stateId: "{endpointName}")] IOnceExecutor<Endpoint> endpointController,
            [SignalR(HubName = "RoutingController")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            var sideEffects = await endpointController.Once(endpoint =>
            {
                if (!endpoint.Instances.TryGetValue(endpointReport.InstanceId, out var instance))
                {
                    instance = new EndpointInstance
                    {
                        InstanceId = endpointReport.InstanceId
                    };
                }

                //TODO: What to do if a designated handler has been removed?
                instance.CommandHandlers = endpointReport.CommandHandlers;
                instance.EventHandlers = endpointReport.EventHandlers;

                endpoint.Validate();

                return ProcessingResult.Ok(null);
            });

            return await sideEffects.Apply(signalRMessages);
        }
        

        [FunctionName(nameof(CommissionCommandRoute))]
        public async Task<IActionResult> CommissionCommandRoute(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] CommissionCommandRouteRequest request,
            [ExactlyOnce(requestId: "{requestId}", stateId: "{endpointName}")] IOnceExecutor<Routes> routesAccess,
            [SignalR(HubName = "RoutingController")] IAsyncCollector<SignalRMessage> signalRMessages,
            [Queue("update-routing")] ICollector<UpdateRoutingTable> updateRoutingCollector)
        {

            CommandHandler handler = await applicationState.ReadObjectAsync<CommandHandler>(request.HandlerAssemblyQualifiedName);

            var sideEffects = await routesAccess.Once(x =>
            {
                x.CommandRoutes[handler.HandledMessageTypeFullName] = handler.Id;

                return ProcessingResult.Ok(null, new SignalRMessage
                {
                    Target = "updateRouting"
                });
            });

            return await sideEffects.Apply(signalRMessages);
        }

        //[FunctionName(nameof(UpdateRouting))]
        //public async Task UpdateRouting(
        //    [QueueTrigger("update-routing")] UpdateRoutingTable updateRouting,
        //    [ExactlyOnce(requestId: "{requestId}", stateId: "RoutingTable")] IOnceExecutor<RoutingTableState> routingTableStateAccess,
        //    [SignalR(HubName = "RoutingController")] IAsyncCollector<SignalRMessage> signalRMessages)
        //{
        //    var eventHandlers = await applicationState.GetItemLinqQueryable<EventHandler>(true, null, new QueryRequestOptions
        //        {
                    
        //            PartitionKey = new PartitionKey(customerId)
        //        })
        //        .Where(c => c.Id != "_transaction")
        //        .ToList();

        //    var sideEffects = await routingTableStateAccess.Once(x =>
        //    {
                
        //    });
        //}

        [FunctionName(nameof(CommissionEventRoute))]
        public async Task CommissionEventRoute(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] CommissionCommandRouteRequest request,
            [ExactlyOnce(requestId: "{requestId}", stateId: "{endpointName}")] IOnceExecutor<Routes> routesAccess,
            [SignalR(HubName = "RoutingController")] IAsyncCollector<SignalRMessage> signalRMessages)
        {
            EventHandler handler = await applicationState.ReadObjectAsync<EventHandler>(request.HandlerAssemblyQualifiedName);

            await routesAccess.Once(x =>
            {
                if (!x.EventRoutes.TryGetValue(handler.HandledMessageTypeFullName, out var handlers))
                {
                    handlers = new HashSet<string>();
                    x.EventRoutes[handler.HandledMessageTypeFullName] = handlers;
                }

                handlers.Add(handler.Id);
            });
        }

        public class UpdateRoutingTable
        {
            public string RequestId { get; set; }
        }

        public class Routes : State
        {
            public Dictionary<string, string> CommandRoutes { get; set; } = new Dictionary<string, string>();
            public Dictionary<string, HashSet<string>> EventRoutes { get; set; } = new Dictionary<string, HashSet<string>>();
        }

        public class EndpointReportProcessingResult
        {

        }

        public class CommissionCommandRouteRequest
        {
            public string RequestId { get; set; }
            public string HandlerAssemblyQualifiedName { get; set; }
        }

        public class CommissionEventRouteRequest
        {
            public string RequestId { get; set; }
            public string HandlerAssemblyQualifiedName { get; set; }
        }

        public class EndpointReportRequest
        {
            public string ReportId { get; set; }
            public string EndpointName { get; set; }
            public string InstanceId { get; set; }
            public List<MessageHandlerInstance> CommandHandlers { get; set; }
            public List<MessageHandlerInstance> EventHandlers { get; set; }
        }
    }

    public class CommandHandler
    {
        [JsonProperty("id")] public string Id { get; set; }
        public string HandledMessageTypeFullName { get; set; }
    }

    public class EventHandler
    {
        [JsonProperty("id")] public string Id { get; set; }
        public string HandledMessageTypeFullName { get; set; }
    }

    public enum AlertSeverity
    {
        Warning,
        Error
    }

    public class RoutingAlert
    {
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; }
    }
}
