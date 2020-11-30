using Azure.Storage.Blobs;
using ExactlyOnce.Routing.Endpoint.Model;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;
using NServiceBus.Settings;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    //TODO: Authorization
    //TODO: DLQ proxy for ServiceControl
    //TODO: Partitioning of Outbox collection
    //TODO: Mixed-mode and migration
    //TODO: Production-quality command line tool
    //TODO: On-premises version (SQL, SignalR Core)

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