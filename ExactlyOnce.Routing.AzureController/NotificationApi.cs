using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.AzureController
{
    public class NotificationApi
    {
        [FunctionName(nameof(Publish))]
        public async Task Publish(
            [QueueTrigger("signalr")] EventMessage eventMessage,
            [SignalR(HubName = "RoutingController")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {
            var e = eventMessage.Payload as RoutingTableChanged;
            if (e == null)
            {
                //We only care about that specific event
                return;
            }

            await signalRMessages.AddAsync(new SignalRMessage
            {
                Target = "routeTableUpdated",
                Arguments = new object[] { new RoutingTableUpdated
                {
                    JsonContent = JsonConvert.SerializeObject(e)
                } }
            });
        }

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "RoutingController")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        public class RoutingTableUpdated
        {
            public string JsonContent { get; set; }
        }
    }
}