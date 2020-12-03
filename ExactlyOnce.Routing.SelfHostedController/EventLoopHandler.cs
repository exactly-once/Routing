using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.Routing.SelfHostedController
{
    public class EventLoopHandler : IMessageHandler
    {
        readonly OnceExecutorFactory executorFactory;
        readonly ILogger<EventLoopHandler> log;

        public EventLoopHandler(OnceExecutorFactory executorFactory, ILogger<EventLoopHandler> log)
        {
            this.executorFactory = executorFactory;
            this.log = log;
        }

        public async Task Handle(EventMessage eventMessage, ISender sender)
        {
            var processor = executorFactory.CreateEventProcessor(eventMessage.UniqueId, eventMessage.DestinationId);

            log.LogInformation(eventMessage.Source != null
                ? $"Processing event {eventMessage.Payload.GetType().Name} published by {eventMessage.Source} with sequence {eventMessage.Sequence} addressed to {eventMessage.DestinationType} {eventMessage.DestinationId}."
                : $"Processing event {eventMessage.Payload.GetType().Name} addressed to {eventMessage.DestinationType} {eventMessage.DestinationId}.");

            var sideEffects = await processor.Once(eventMessage).ConfigureAwait(false);

            foreach (var message in sideEffects)
            {
                await sender.Publish(message).ConfigureAwait(false);
            }
        }
    }
}