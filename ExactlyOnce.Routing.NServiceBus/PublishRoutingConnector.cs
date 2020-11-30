using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Pipeline;
using NServiceBus.Unicast.Queuing;

namespace ExactlyOnce.Routing.NServiceBus
{
    class PublishRoutingConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        static readonly ILog logger = LogManager.GetLogger<PublishRoutingConnector>();
        readonly RoutingLogic router;

        public PublishRoutingConnector(RoutingLogic router)
        {
            this.router = router;
        }

        public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var eventType = context.Message.MessageType;
            var routingStrategies = router.Route(eventType, context).ToList();
            if (routingStrategies.Count == 0)
            {
                logger.Warn($"No subscribers for message {eventType}.");
                return;
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Publish.ToString();

            try
            {
                await stage(this.CreateOutgoingLogicalMessageContext(context.Message, routingStrategies, context)).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({eventType}) in the MessageEndpointMappings of the UnicastBusConfig section in the configuration file. It may also be the case that the given queue hasn\'t been created yet, or has been deleted.", ex);
            }
        }
    }
}