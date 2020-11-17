using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Pipeline;

namespace ExactlyOnce.Routing.NServiceBus
{
    class NullSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        protected override Task Terminate(ISubscribeContext context)
        {
            log.Debug($"Subscribe was called for {context.EventType.FullName}. With ExactlyOnce.Routing, unsubscribe operations have no effect.");
            return Task.FromResult(0);
        }

        static readonly ILog log = LogManager.GetLogger<NullSubscribeTerminator>();
    }
}
