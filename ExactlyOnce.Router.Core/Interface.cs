using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Logging;
using NServiceBus.Transport;

namespace ExactlyOnce.Router.Core
{
    interface Interface
    {
        string Name { get; }
        Task<IRawEndpoint> Initialize(Func<MessageContext, string, IMessageRoutingContext> contextFactory);
        Task StartReceiving();
        Task StopReceiving();
        Task Stop();
    }

    class Interface<T> : Interface where T : TransportDefinition, new()
    {
        public string Name { get; }
        public Interface(string endpointName, string interfaceName, Action<TransportExtensions<T>> transportCustomization, Func<IMessageRoutingContext, Task> onMessage, string poisonQueue, int? maximumConcurrency, bool autoCreateQueues, string autoCreateQueuesIdentity, int immediateRetries, int delayedRetries, int circuitBreakerThreshold)
        {
            Name = interfaceName;
            rawConfig = new ThrottlingRawEndpointConfig<T>(endpointName, poisonQueue, ext =>
                {
                    SetTransportSpecificFlags(ext.GetSettings(), poisonQueue);
                    transportCustomization?.Invoke(ext);
                },
                async (context, _) =>
                {
                    var watch = new Stopwatch();
                    watch.Start();
                    var routingContext = contextFactory(context, Name);
                    await onMessage(routingContext).ConfigureAwait(false);
                    watch.Stop();
                    //RouterEventSource.Instance.MessageProcessed(endpointName, interfaceName, watch.ElapsedMilliseconds);
                },
                (context, dispatcher) =>
                {
                    log.Error("Moving poison message to the error queue", context.Error.Exception);
                    return context.MoveToErrorQueue(poisonQueue);
                },
                context =>
                {
                    //RouterEventSource.Instance.MessageFailed(endpointName, interfaceName);
                },
                maximumConcurrency,
                immediateRetries, delayedRetries, circuitBreakerThreshold, autoCreateQueues, autoCreateQueuesIdentity);
        }

        static void SetTransportSpecificFlags(NServiceBus.Settings.SettingsHolder settings, string poisonQueue)
        {
            settings.Set("errorQueue", poisonQueue);
            settings.Set("RabbitMQ.RoutingTopologySupportsDelayedDelivery", true);
        }

        public async Task<IRawEndpoint> Initialize(Func<MessageContext, string, IMessageRoutingContext> contextFactory)
        {
            this.contextFactory = contextFactory;
            sender = await rawConfig.Create().ConfigureAwait(false);
            return sender;
        }

        public async Task StartReceiving()
        {
            receiver = await sender.Start().ConfigureAwait(false);
        }

        public async Task StopReceiving()
        {
            if (receiver != null)
            {
                stoppable = await receiver.StopReceiving().ConfigureAwait(false);
            }
            else
            {
                stoppable = null;
            }
        }

        public async Task Stop()
        {
            if (stoppable != null)
            {
                await stoppable.Stop().ConfigureAwait(false);
                stoppable = null;
            }
        }

        static ILog log = LogManager.GetLogger(typeof(Interface));
        IReceivingRawEndpoint receiver;
        IStartableRawEndpoint sender;
        IStoppableRawEndpoint stoppable;

        ThrottlingRawEndpointConfig<T> rawConfig;
        Func<MessageContext, string, IMessageRoutingContext> contextFactory;
    }
}