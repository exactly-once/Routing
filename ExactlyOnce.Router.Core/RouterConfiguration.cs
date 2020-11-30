using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;

namespace ExactlyOnce.Router.Core
{
    /// <summary>
    /// Constructs the router.
    /// </summary>
    public class RouterConfiguration
    {
        /// <summary>
        /// Router endpoint name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates new router configuration with provided endpoint name.
        /// </summary>
        /// <param name="name"></param>
        public RouterConfiguration(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Adds a new interface to the router.
        /// </summary>
        /// <typeparam name="T">Transport to use for this interface.</typeparam>
        /// <param name="name">Name of the interface.</param>
        /// <param name="customization">A callback for customizing the transport settings.</param>
        /// <param name="onMessage">Callback for processing messages arriving at this interface.</param>
        public InterfaceConfiguration<T> AddInterface<T>(
            string name, 
            Action<TransportExtensions<T>> customization,
            Func<IMessageRoutingContext, Task> onMessage)
            where T : TransportDefinition, new()
        {
            var ifaceConfig = new InterfaceConfiguration<T>(name, customization, onMessage);
            InterfaceFactories.Add(() => CreateInterface(ifaceConfig));
            return ifaceConfig;
        }

        /// <summary>
        /// Adds a new send-only interface to the router.
        /// </summary>
        /// <typeparam name="T">Transport to use for this interface.</typeparam>
        /// <param name="name">Name of the interface.</param>
        /// <param name="customization">A callback for customizing the transport settings.</param>
        public SendOnlyInterfaceConfiguration<T> AddSendOnlyInterface<T>(string name, Action<TransportExtensions<T>> customization)
            where T : TransportDefinition, new()
        {
            var ifaceConfig = new SendOnlyInterfaceConfiguration<T>(name, customization, this);
            SendOnlyInterfaceFactories.Add(() => CreateSendOnlyInterface(ifaceConfig));
            return ifaceConfig;
        }

        SendOnlyInterface CreateSendOnlyInterface<T>(SendOnlyInterfaceConfiguration<T> ifaceConfig)
            where T : TransportDefinition, new()
        {
            return ifaceConfig.Create(Name);
        }

        Interface CreateInterface<T>(InterfaceConfiguration<T> ifaceConfig) where T : TransportDefinition, new()
        {
            return ifaceConfig.Create(Name, PoisonQueueName, autoCreateQueues, autoCreateQueuesIdentity, ImmediateRetries, DelayedRetries, CircuitBreakerThreshold);
        }

        /// <summary>
        /// Configures the router to automatically create a queue when starting up.
        /// </summary>
        /// <param name="identity">Identity to use when creating the queue.</param>
        public void AutoCreateQueues(string identity = null)
        {
            autoCreateQueues = true;
            autoCreateQueuesIdentity = identity;
        }

        /// <summary>
        /// Gets or sets the number of immediate retries to use when resolving failures during forwarding.
        /// </summary>
        public int ImmediateRetries { get; set; } = 5;

        /// <summary>
        /// Gets or sets the number of delayed retries to use when resolving failures during forwarding.
        /// </summary>
        public int DelayedRetries { get; set; } = 5;

        /// <summary>
        /// Gets or sets the number of consecutive failures required to trigger the throttled mode.
        /// </summary>
        public int CircuitBreakerThreshold { get; set; } = 5;

        /// <summary>
        /// Gets or sets the name of the poison queue.
        /// </summary>
        public string PoisonQueueName { get; set; } = "poison";

        bool? autoCreateQueues;
        string autoCreateQueuesIdentity;
        internal List<Func<Interface>> InterfaceFactories = new List<Func<Interface>>();
        internal List<Func<SendOnlyInterface>> SendOnlyInterfaceFactories = new List<Func<SendOnlyInterface>>();
    }
}