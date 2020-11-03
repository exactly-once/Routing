using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.Routing.AzureController
{
    public class NotificationApi
    {
        [FunctionName(nameof(PublishRouteTable))]
        public async Task PublishRouteTable(
            [QueueTrigger("routing-updates")] EventMessage eventMessage,
            [SignalR(HubName = "RoutingController")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log)
        {

            await signalRMessages.AddAsync(new SignalRMessage
            {
                //TODO
            });
        }

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo GetSignalRInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "RoutingController")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }
    }
}