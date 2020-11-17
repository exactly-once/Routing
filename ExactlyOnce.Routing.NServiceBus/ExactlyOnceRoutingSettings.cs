using Azure.Storage.Blobs;

// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public class ExactlyOnceRoutingSettings
    {
        internal string ControllerUrl { get; set; }
        internal BlobContainerClient ControllerContainerClient { get; set; }
        internal string RouterName { get; private set; }
        internal string SiteName { get; private set; }

        /// <summary>
        /// Declare that this endpoint communicates with other sites via the specified router.
        /// </summary>
        public void ConnectToRouter(string localRouterName)
        {
            RouterName = localRouterName;
        }

        /// <summary>
        /// Declare that this endpoint is part of the specified site. The endpoint is not connected to the router
        /// and will only be able to communicate with endpoints in the same site.
        /// </summary>
        public void SetSiteName(string siteName)
        {
            SiteName = siteName;
        }
    }
}