using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Unicast.Queuing;

namespace ExactlyOnce.Routing.NServiceBus
{
    class LegacyNativePublishRoutingConnector : StageConnector<IOutgoingPublishContext, IOutgoingLogicalMessageContext>
    {
        readonly RoutingLogic router;

        public LegacyNativePublishRoutingConnector(RoutingLogic router)
        {
            this.router = router;
        }

        public override async Task Invoke(IOutgoingPublishContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var eventType = context.Message.MessageType;
            var managedRoutingStrategies = router.Route(eventType, context).ToArray();

            var managedDestinations = managedRoutingStrategies
                .Select(x => x.RoutingSlip.DestinationEndpoint)
                .ToArray();

            var routingStrategies = new List<RoutingStrategy>(managedRoutingStrategies)
            {
                new DualRoutingMulticastRoutingStrategy(eventType, managedDestinations)
            };

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

        class DualRoutingMulticastRoutingStrategy : RoutingStrategy
        {
            readonly Type eventType;
            readonly string[] managedDestinations;

            public DualRoutingMulticastRoutingStrategy(Type eventType, string[] managedDestinations)
            {
                this.eventType = eventType;
                this.managedDestinations = managedDestinations;
            }

            public override AddressTag Apply(Dictionary<string, string> headers)
            {
                foreach (var destination in managedDestinations)
                {
                    headers[$"ExactlyOnce.Routing.DualRouting-{destination}"] = "1";
                }
                return new MulticastAddressTag(eventType);
            }
        }
    }
}