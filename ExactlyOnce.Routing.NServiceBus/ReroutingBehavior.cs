using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Transport;

namespace ExactlyOnce.Routing.NServiceBus
{
    class ReroutingBehavior : ForkConnector<IIncomingPhysicalMessageContext, IRoutingContext>
    {
        readonly RoutingLogic routingLogic;

        public ReroutingBehavior(RoutingLogic routingLogic)
        {
            this.routingLogic = routingLogic;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next, Func<IRoutingContext, Task> fork)
        {
            var newRoutingStrategy = routingLogic.CheckIfReroutingIsNeeded(context);
            if (newRoutingStrategy == null)
            {
                await next().ConfigureAwait(false);
                return;
            }

            var processedMessage = new OutgoingMessage(context.Message.MessageId, new Dictionary<string, string>(context.Message.Headers), context.Message.Body);
            var forwardingContext = new ReroutingContext(processedMessage, newRoutingStrategy, context);
            await fork(forwardingContext).ConfigureAwait(false);
        }
    }
}