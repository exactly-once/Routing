using Azure.Storage.Blobs;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;
using NServiceBus.Settings;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
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
            string controllerUrl /*TODO: Auth?*/)
        {
            config.DisableFeature<AutoSubscribe>();
            config.GetSettings().EnableFeatureByDefault<ExactlyOnceRoutingFeature>();

            var settings = config.GetSettings().GetOrCreate<ExactlyOnceRoutingSettings>();

            settings.ControllerUrl = controllerUrl;
            settings.ControllerContainerClient = controllerContainerClient;

            return settings;
        }
    }
}