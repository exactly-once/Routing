using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Pipeline;
using NServiceBus.Unicast.Queuing;

namespace ExactlyOnce.Routing.NServiceBus
{
    class SendRoutingConnector : StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext>
    {
        readonly RoutingLogic router;

        public SendRoutingConnector(RoutingLogic router)
        {
            this.router = router;
        }

        public override async Task Invoke(IOutgoingSendContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var messageType = context.Message.MessageType;
            var routingStrategies = router.Route(messageType, context).ToList();
            if (routingStrategies.Count == 0)
            {
                throw new Exception($"No routes found for message {messageType.AssemblyQualifiedName}");
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