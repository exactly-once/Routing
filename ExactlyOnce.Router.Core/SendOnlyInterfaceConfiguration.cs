using System;
using NServiceBus;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace ExactlyOnce.Router.Core
{
    /// <summary>
    /// Configures the switch port.
    /// </summary>
    /// <typeparam name="T">Type of transport.</typeparam>
    public class SendOnlyInterfaceConfiguration<T>
        where T : TransportDefinition, new()
    {
        Action<TransportExtensions<T>> customization;
        string overriddenEndpointName;

        /// <summary>
        /// Name of the interface.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Router's configuration.
        /// </summary>
        public RouterConfiguration RouterConfiguration { get; }

        internal SendOnlyInterfaceConfiguration(string name, Action<TransportExtensions<T>> customization, RouterConfiguration routerConfiguration)
        {
            Name = name;
            this.customization = customization;
            RouterConfiguration = routerConfiguration;
        }

        /// <summary>
        /// Overrides the interface endpoint name.
        /// </summary>
        /// <param name="interfaceEndpointName">Endpoint name to use for this interface instead of Router's name</param>
        public void OverrideEndpointName(string interfaceEndpointName)
        {
            overriddenEndpointName = interfaceEndpointName;
        }

        /// <summary>
        /// Distribution policy of the port.
        /// </summary>
        public RawDistributionPolicy DistributionPolicy { get; } = new RawDistributionPolicy();

        /// <summary>
        /// Physical routing settings of the port.
        /// </summary>
        public EndpointInstances EndpointInstances { get; } = new EndpointInstances();

        internal SendOnlyInterface Create(string endpointName)
        {
            return new SendOnlyInterface<T>(overriddenEndpointName ?? endpointName, Name, customization);
        }
    }
}
