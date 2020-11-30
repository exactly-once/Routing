using System;
using Azure.Storage.Blobs;
using ExactlyOnce.Routing.Endpoint.Model;
using NServiceBus;
using NServiceBus.Transport;
using IDistributionPolicy = ExactlyOnce.Routing.Endpoint.Model.IDistributionPolicy;

namespace ExactlyOnce.Router
{
    /// <summary>
    /// Constructs the router.
    /// </summary>
    public class RouterConfiguration
    {
        internal readonly string InstanceId;
        internal readonly BlobContainerClient ControllerContainerClient;
        internal readonly string ControllerUrl;
        internal readonly Core.RouterConfiguration RouterConfig;
        internal readonly RoutingLogic RoutingLogic = new RoutingLogic();

        internal DistributionPolicyConfiguration DistributionPolicyConfiguration = new DistributionPolicyConfiguration();

        /// <summary>
        /// Creates new router configuration with provided endpoint name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="controllerContainerClient">Blob client for accessing routing table snapshot.</param>
        /// <param name="controllerUrl">Root URL of the routing controller.</param>
        public RouterConfiguration(string name, string instanceId, BlobContainerClient controllerContainerClient, string controllerUrl)
        {
            this.InstanceId = instanceId;
            this.ControllerContainerClient = controllerContainerClient;
            this.ControllerUrl = controllerUrl;
            RouterConfig = new Core.RouterConfiguration(name);
        }

        /// <summary>
        /// Adds a new interface to the router.
        /// </summary>
        /// <typeparam name="T">Transport to use for this interface.</typeparam>
        /// <param name="name">Name of the interface.</param>
        /// <param name="customization">A callback for customizing the transport settings.</param>
        public InterfaceConfiguration<T> AddInterface<T>(
            string name, 
            Action<TransportExtensions<T>> customization)
            where T : TransportDefinition, new()
        {
            return new InterfaceConfiguration<T>(RouterConfig.AddInterface(name, customization, RoutingLogic.HandleMessage));
        }

        /// <summary>
        /// Configures the router to automatically create a queue when starting up.
        /// </summary>
        /// <param name="identity">Identity to use when creating the queue.</param>
        public void AutoCreateQueues(string identity = null)
        {
            RouterConfig.AutoCreateQueues(identity);
        }

        /// <summary>
        /// Gets or sets the number of immediate retries to use when resolving failures during forwarding.
        /// </summary>
        public int ImmediateRetries {
            get => RouterConfig.ImmediateRetries;
            set => RouterConfig.ImmediateRetries = value;
        }

        /// <summary>
        /// Gets or sets the number of delayed retries to use when resolving failures during forwarding.
        /// </summary>
        public int DelayedRetries
        {
            get => RouterConfig.DelayedRetries;
            set => RouterConfig.DelayedRetries = value;
        }

        /// <summary>
        /// Gets or sets the number of consecutive failures required to trigger the throttled mode.
        /// </summary>
        public int CircuitBreakerThreshold
        {
            get => RouterConfig.CircuitBreakerThreshold;
            set => RouterConfig.CircuitBreakerThreshold = value;
        }

        /// <summary>
        /// Gets or sets the name of the poison queue.
        /// </summary>
        public string PoisonQueueName
        {
            get => RouterConfig.PoisonQueueName;
            set => RouterConfig.PoisonQueueName = value;
        }

        /// <summary>
        /// Registers a custom distribution policy.
        /// </summary>
        /// <param name="name">Name of the endpoint or router.</param>
        /// <param name="policyFactory">Factory for the policy objects.</param>
        public void AddDistributionPolicy(string name, Func<IDistributionPolicy> policyFactory)
        {
            DistributionPolicyConfiguration.AddDistributionPolicy(name, policyFactory);
        }
    }
}