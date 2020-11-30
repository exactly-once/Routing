using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Transport;

namespace ExactlyOnce.Router.Core
{
    interface SendOnlyInterface
    {
        string Name { get; }
        Task<IRawEndpoint> Initialize();
        Task Stop();
    }

    class SendOnlyInterface<T> : SendOnlyInterface where T : TransportDefinition, new()
    {
        public SendOnlyInterface(string endpointName, string interfaceName, Action<TransportExtensions<T>> transportCustomization)
        {
            Name = interfaceName;
            config = RawEndpointConfiguration.CreateSendOnly(endpointName);
            var transport = config.UseTransport<T>();
            SetTransportSpecificFlags(transport.GetSettings());
            transportCustomization?.Invoke(transport);
        }

        public string Name { get; }

        static void SetTransportSpecificFlags(NServiceBus.Settings.SettingsHolder settings)
        {
            settings.Set("RabbitMQ.RoutingTopologySupportsDelayedDelivery", true);
        }

        public async Task<IRawEndpoint> Initialize()
        {
            var startable = await RawEndpoint.Create(config).ConfigureAwait(false);
            config = null;

            endpoint = await startable.Start().ConfigureAwait(false);
            return startable;
        }

        public async Task Stop()
        {
            if (endpoint != null)
            {
                await endpoint.Stop().ConfigureAwait(false);
                endpoint = null;
            }
        }

        RawEndpointConfiguration config;
        IStoppableRawEndpoint endpoint;
    }
}