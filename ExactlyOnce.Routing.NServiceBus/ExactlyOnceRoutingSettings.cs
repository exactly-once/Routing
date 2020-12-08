using System;
using Azure.Storage.Blobs;
using ExactlyOnce.Routing.Endpoint.Model;
using ExactlyOnceDistributionPolicy = ExactlyOnce.Routing.Endpoint.Model.IDistributionPolicy;


// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    public class ExactlyOnceRoutingSettings
    {
        internal string ControllerUrl { get; set; }
        internal BlobContainerClient ControllerContainerClient { get; set; }
        internal string RouterName { get; private set; }
        internal string SiteName { get; private set; }
        public LegacyMigrationSettings LegacyMigration { get; } = new LegacyMigrationSettings();

        internal SiteRoutingPolicyConfiguration SiteRoutingPolicyConfiguration = new SiteRoutingPolicyConfiguration();
        internal DistributionPolicyConfiguration DistributionPolicyConfiguration = new DistributionPolicyConfiguration();

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

        public void AddSiteRoutingPolicy(string name, Func<ISiteRoutingPolicy> policyFactory)
        {
            SiteRoutingPolicyConfiguration.AddSiteRoutingPolicy(name, policyFactory);
        }

        public void AddDistributionPolicy(string name, Func<ExactlyOnceDistributionPolicy> policyFactory)
        {
            DistributionPolicyConfiguration.AddDistributionPolicy(name, policyFactory);
        }
    }
}