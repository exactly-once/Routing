using System.Collections.Generic;
using NServiceBus.Extensibility;
using NServiceBus.ObjectBuilder;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace ExactlyOnce.Routing.NServiceBus
{
    class ReroutingContext : ContextBag, IRoutingContext
    {
        public ReroutingContext(
            OutgoingMessage messageToDispatch, 
            RoutingStrategy routingStrategy, 
            IBehaviorContext parentContext)
            : base(parentContext?.Extensions)
        {
            Message = messageToDispatch;
            RoutingStrategies = new []{routingStrategy};
        }

        public OutgoingMessage Message { get; }
        public IReadOnlyCollection<RoutingStrategy> RoutingStrategies { get; set; }
        public IBuilder Builder => Get<IBuilder>();
        public ContextBag Extensions => this;
    }
}