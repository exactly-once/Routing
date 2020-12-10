using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Unicast.Messages;
using NServiceBus.Unicast.Queuing;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

namespace ExactlyOnce.Routing.NServiceBus
{
    class LegacyStorageDrivenPublishRoutingConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        static readonly ILog logger = LogManager.GetLogger<LegacyStorageDrivenPublishRoutingConnector>();
        readonly RoutingLogic router;
        readonly ISubscriptionStorage subscriptionStorage;
        readonly MessageMetadataRegistry messageMetadataRegistry;

        public LegacyStorageDrivenPublishRoutingConnector(RoutingLogic router, ISubscriptionStorage subscriptionStorage, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.router = router;
            this.subscriptionStorage = subscriptionStorage;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var eventType = context.Message.MessageType;
            var routingStrategies = router.Route(eventType, context).ToList();

            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(eventType).MessageHierarchy;
            var legacySubscribers =
                await subscriptionStorage.GetSubscriberAddressesForMessage(typesToRoute.Select(x => new MessageType(x)), context.Extensions)
                    .ConfigureAwait(false);

            var combinedRoutingStrategies = Deduplicate(routingStrategies, legacySubscribers);

            if (combinedRoutingStrategies.Count == 0)
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

        static List<RoutingStrategy> Deduplicate(IReadOnlyCollection<RoutingSlipRoutingStrategy> managedRoutingStrategies, IEnumerable<Subscriber> legacySubscribers)
        {
            var result = new List<RoutingStrategy>(managedRoutingStrategies);

            foreach (var subscriber in legacySubscribers)
            {
                var legacySubscriberEndpoint = subscriber.Endpoint ?? subscriber.TransportAddress.Split('@')[0];

                if (managedRoutingStrategies.All(x => x.RoutingSlip.DestinationEndpoint != legacySubscriberEndpoint))
                {
                    result.Add(new UnicastRoutingStrategy(subscriber.TransportAddress));
                }
            }

            return result;
        }
    }
}