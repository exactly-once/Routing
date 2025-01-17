﻿using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Unicast.Queuing;

namespace ExactlyOnce.Routing.NServiceBus
{
    class SendRoutingConnector : StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext>
    {
        readonly RoutingLogic routingLogic;
        readonly LegacyRoutingLogic legacyRoutingLogic;

        public SendRoutingConnector(RoutingLogic routingLogic, LegacyRoutingLogic legacyRoutingLogic)
        {
            this.routingLogic = routingLogic;
            this.legacyRoutingLogic = legacyRoutingLogic;
        }

        public override async Task Invoke(IOutgoingSendContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var messageType = context.Message.MessageType;
            var routingStrategies = routingLogic.Route(messageType, context).ToList<RoutingStrategy>();

            if (routingStrategies.Count == 0 && legacyRoutingLogic != null)
            {
                routingStrategies = legacyRoutingLogic.Route(context);
            }

            if (routingStrategies.Count == 0)
            {
                throw new Exception($"No routes found for message {messageType.FullName}");
            }

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Send.ToString();

            try
            {
                await stage(this.CreateOutgoingLogicalMessageContext(context.Message, routingStrategies, context)).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. The destination may be misconfigured for this kind of message ({messageType}) in the MessageEndpointMappings of the UnicastBusConfig section in the configuration file. It may also be the case that the given queue hasn\'t been created yet, or has been deleted.", ex);
            }
        }
    }
}