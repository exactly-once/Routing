using System.Threading.Tasks;
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
            await signalRMessages.AddAsync(new SignalRMessage
            {
                Target = "newMessage",
                Arguments = new[] { JsonConvert.SerializeObject(eventMessage.Payload) }
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