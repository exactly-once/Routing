using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace ExactlyOnce.Routing.NServiceBus
{
    class DualRoutingDeduplicationBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        readonly string dualRoutingKey;

        public DualRoutingDeduplicationBehavior(string endpointName)
        {
            dualRoutingKey = $"ExactlyOnce.Routing.DualRouting-{endpointName}";
        }

        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            if (context.Message.Headers.ContainsKey(dualRoutingKey))
            {
                //The same message has also been sent via unicast routing so we can ignore this instance
                return Task.CompletedTask;
            }

            return next();
        }
    }
}