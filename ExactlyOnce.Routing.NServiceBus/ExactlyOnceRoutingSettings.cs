using System;
using System.Collections.Generic;
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
        internal Dictionary<string, Func<ISiteRoutingPolicy>> RoutingPolicies = new Dictionary<string, Func<ISiteRoutingPolicy>>
        {
            {"default", () => new RouteToMostRecentlyAddedSiteRoutingPolicy()},
            {"explicit", () => new ExplicitSiteRoutingPolicy()},
            {"round-robin", () => new RoundRobinSiteRoutingPolicy()},
            {"most-recently-added", () => new RouteToMostRecentlyAddedSiteRoutingPolicy()},
            {"nearest", () => new RouteToNearestSiteRoutingPolicy()},
        };
        internal Dictionary<string, Func<ExactlyOnceDistributionPolicy>> DistributionPolicies = new Dictionary<string, Func<ExactlyOnceDistributionPolicy>>()
        {
            {"default", () => new RoundRobinDistributionPolicy()},
            {"round-robin", () => new RoundRobinDistributionPolicy()}
        };

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
            RoutingPolicies.Add(name, policyFactory);
        }

        public void AddDistributionPolicy(string name, Func<ExactlyOnceDistributionPolicy> policyFactory)
        {
            DistributionPolicies.Add(name, policyFactory);
        }
    }
}