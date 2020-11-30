using NServiceBus.Transport;

namespace ExactlyOnce.Router
{
    /// <summary>
    /// Configures the switch port.
    /// </summary>
    /// <typeparam name="T">Type of transport.</typeparam>
    public class InterfaceConfiguration<T>
        where T : TransportDefinition, new()
    {
        readonly Core.InterfaceConfiguration<T> interfaceConfig;

        internal InterfaceConfiguration(Core.InterfaceConfiguration<T> interfaceConfig)
        {
            this.interfaceConfig = interfaceConfig;
        }

        /// <summary>
        /// Configures the port to automatically create a queue when starting up. Overrides switch-level setting.
        /// </summary>
        /// <param name="identity">Identity to use when creating the queue.</param>
        public void AutoCreateQueues(string identity = null)
        {
            interfaceConfig.AutoCreateQueues(identity);
        }

        /// <summary>
        /// Overrides the poison message queue name.
        /// </summary>
        public void OverridePoisonQueue(string poisonQueue)
        {
            interfaceConfig.OverridePoisonQueue(poisonQueue);
        }

        /// <summary>
        /// Overrides the interface endpoint name.
        /// </summary>
        /// <param name="interfaceEndpointName">Endpoint name to use for this interface instead of Router's name</param>
        public void OverrideEndpointName(string interfaceEndpointName)
        {
            interfaceConfig.OverrideEndpointName(interfaceEndpointName);
        }

        /// <summary>
        /// Limits the processing concurrency of the port to a given value.
        /// </summary>
        /// <param name="maximumConcurrency">Maximum level of concurrency for the port's transport.</param>
        public void LimitMessageProcessingConcurrencyTo(int maximumConcurrency)
        {
            interfaceConfig.LimitMessageProcessingConcurrencyTo(maximumConcurrency);
        }
    }
}
