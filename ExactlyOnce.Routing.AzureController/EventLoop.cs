using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.Routing.AzureController
{
    public class EventLoop
    {
        [FunctionName(nameof(ProcessEventMessage))]
        public async Task ProcessEventMessage(
            [QueueTrigger("event-queue")] EventMessage eventMessage, 
            [ExactlyOnce(requestId: "{uniqueId}", stateId: "{destinationId}")] IOnceEventProcessor execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector,
            [Queue("signalr")] ICollector<EventMessage> signalrCollector,
            ILogger log)
        {
            log.LogInformation(eventMessage.Source != null
                ? $"Processing event published by {eventMessage.Source} with sequence {eventMessage.Sequence} addressed to {eventMessage.DestinationType} {eventMessage.DestinationId}."
                : $"Processing event published addressed to {eventMessage.DestinationType} {eventMessage.DestinationId}.");

            var sideEffects = await execute.Once(eventMessage).ConfigureAwait(false);
            foreach (var message in sideEffects)
            {
                if (message.DestinationType == typeof(NotificationApi).FullName) //HACK
                {
                    signalrCollector.Add(message);
                }
                else
                {
                    eventCollector.Add(message);
                }
            }
        }
    }
}