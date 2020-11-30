using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace ExactlyOnce.Router.Core
{
    /// <summary>
    /// Configures the switch port.
    /// </summary>
    /// <typeparam name="T">Type of transport.</typeparam>
    public class InterfaceConfiguration<T>
        where T : TransportDefinition, new()
    {
        Action<TransportExtensions<T>> customization;
        readonly Func<IMessageRoutingContext, Task> onMessage;
        bool? autoCreateQueues;
        string autoCreateQueuesIdentity;
        int? maximumConcurrency;
        string overriddenEndpointName;
        string overriddenPoisonQueue;
        string name;

        internal InterfaceConfiguration(string name, Action<TransportExtensions<T>> customization, Func<IMessageRoutingContext, Task> onMessage)
        {
            this.name = name;
            this.customization = customization;
            this.onMessage = onMessage;
        }

        /// <summary>
        /// Configures the port to automatically create a queue when starting up. Overrides switch-level setting.
        /// </summary>
        /// <param name="identity">Identity to use when creating the queue.</param>
        public void AutoCreateQueues(string identity = null)
        {
            autoCreateQueues = true;
            autoCreateQueuesIdentity = identity;
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
        /// Overrides the poison message queue name.
        /// </summary>
        public void OverridePoisonQueue(string poisonQueue)
        {
            overriddenPoisonQueue = poisonQueue;
        }

        /// <summary>
        /// Limits the processing concurrency of the port to a given value.
        /// </summary>
        /// <param name="maximumConcurrency">Maximum level of concurrency for the port's transport.</param>
        public void LimitMessageProcessingConcurrencyTo(int maximumConcurrency)
        {
            this.maximumConcurrency = maximumConcurrency;
        }

        internal Interface Create(string endpointName, string poisonQueue, bool? routerAutoCreateQueues, string routerAutoCreateQueuesIdentity, int immediateRetries, int delayedRetries, int circuitBreakerThreshold)
        {
            return new Interface<T>(overriddenEndpointName ?? endpointName, name, customization, onMessage, overriddenPoisonQueue ?? poisonQueue, 
                maximumConcurrency, autoCreateQueues ?? routerAutoCreateQueues ?? false, autoCreateQueuesIdentity ?? routerAutoCreateQueuesIdentity, 
                immediateRetries, delayedRetries, circuitBreakerThreshold);
        }
    }
}
