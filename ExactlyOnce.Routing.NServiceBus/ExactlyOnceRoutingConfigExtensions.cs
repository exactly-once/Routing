using Azure.Storage.Blobs;
using ExactlyOnce.Routing.Endpoint.Model;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    //TODO: Generic query API (search, list_all, get_details)
    //TODO: Production-quality command line tool
    //TODO: Visualizations (graphviz)
    //TODO: SQL persistence
    //TODO: Authorization
    //TODO: DLQ proxy for ServiceControl

    //TODO: Change notifications
    //Add new signalrR hub that will post events when an entity changes. These notifications will be subscribed by the client (web or console)
    //And the client will be re-loading entities when they change. This way the client will always have the current view of the system
    //Filtering etc will always be done on the client as the amount of data expected to be there is going to be relatively small
    //(up to hundreds of items of each type)

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