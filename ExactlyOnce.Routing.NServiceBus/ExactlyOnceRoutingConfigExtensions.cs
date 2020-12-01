using Azure.Storage.Blobs;
using ExactlyOnce.Routing.Endpoint.Model;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    //TODO: Self-hosted version (SQL, SignalR Core)
    //TODO: Acceptance tests
    //TODO: Mixed-mode and migration
    //TODO: Authorization
    //TODO: Visualizations (graphviz)
    //TODO: DLQ proxy for ServiceControl
    //TODO: Partitioning of Outbox collection
    //TODO: Production-quality command line tool

    /// <summary>
    /// Extensions for configuring blueprint-based routing functionality.
    /// </summary>
    public static class ExactlyOnceRoutingConfigExtensions
    {
        /// <summary>
        /// Enables routing configured with the system map.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="controllerContainerClient"></param>
        /// <param name="controllerUrl">URL of the routing controller.</param>
        public static ExactlyOnceRoutingSettings UseExactlyOnceRouting(this EndpointConfiguration config, 
            BlobContainerClient controllerContainerClient,
            string controllerUrl)
        {
            config.DisableFeature<AutoSubscribe>();
            config.GetSettings().EnableFeatureByDefault<ExactlyOnceRoutingFeature>();

            var settings = config.GetSettings().GetOrCreate<ExactlyOnceRoutingSettings>();

            settings.ControllerUrl = controllerUrl;
            settings.ControllerContainerClient = controllerContainerClient;

            config.GetSettings().AddUnrecoverableException(typeof(MoveToDeadLetterQueueException));

            return settings;
        }
    }
}