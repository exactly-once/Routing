using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Pipeline;

namespace ExactlyOnce.Routing.NServiceBus
{
    class NullUnsubscribeTerminator : PipelineTerminator<IUnsubscribeContext>
    {
        protected override Task Terminate(IUnsubscribeContext context)
        {
            log.Debug($"Unsubscribe was called for {context.EventType.FullName}. With ExactlyOnce.Routing, unsubscribe operations have no effect.");
            return Task.FromResult(0);
        }

        static readonly ILog log = LogManager.GetLogger<NullUnsubscribeTerminator>();
    }
}