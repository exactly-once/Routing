using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class DistributionPolicy
    {
        readonly Dictionary<string, IDistributionPolicy> routerDistributionPolicies;
        readonly Dictionary<string, IDistributionPolicy> endpointDistributionPolicies;

        public DistributionPolicy(RoutingTable routingTable,
            Dictionary<string, Func<IDistributionPolicy>> distributionPolicyFactories)
        {
            var endpoints = routingTable.Sites
                .SelectMany(x => x.Value)
                .Select(x => x.EndpointName)
                .Distinct();

            endpointDistributionPolicies = endpoints.ToDictionary(x => x,
                x => InitializeDistributionPolicy(routingTable, distributionPolicyFactories, x));

            routerDistributionPolicies = routingTable.RouterInstances.ToDictionary(x => x.Key,
                x => InitializeDistributionPolicyForRouter(routingTable, distributionPolicyFactories, x.Key, x.Value));
        }

        static IDistributionPolicy InitializeDistributionPolicyForRouter(RoutingTable routingTable, Dictionary<string, Func<IDistributionPolicy>> distributionPolicyFactories,
            string routerName, List<RouterInstanceInfo> instances)
        {
            routingTable.DistributionPolicy.TryGetValue(routerName, out var policyName);
            if (!distributionPolicyFactories.TryGetValue(policyName ?? "default", out var policyFactory))
            {
                throw new Exception($"Unsupported distribution policy {policyName}");
            }

            var queueMap = new Dictionary<string, Dictionary<string, string>>();

            foreach (var routerInstance in instances)
            {
                foreach (var (siteName, inputQueue) in routerInstance.SiteToInputQueueMap)
                {
                    if (!queueMap.TryGetValue(siteName, out var site))
                    {
                        site = new Dictionary<string, string>();
                        queueMap[siteName] = site;
                    }

                    site[routerInstance.InstanceId] = inputQueue;
                }
            }

            var policy = policyFactory();
            policy.Initialize(routerName, queueMap);
            return policy;
        }

        static IDistributionPolicy InitializeDistributionPolicy(RoutingTable routingTable, Dictionary<string, Func<IDistributionPolicy>> distributionPolicyFactories,
            string endpointName)
        {
            routingTable.DistributionPolicy.TryGetValue(endpointName, out var policyName);
            if (!distributionPolicyFactories.TryGetValue(policyName ?? "default", out var policyFactory))
            {
                throw new Exception($"Unsupported distribution policy {policyName}");
            }

            var queueMap = routingTable.Sites
                .Where(s => s.Value.Any(x => x.EndpointName == endpointName)) //Take sites that host instances of target endpoint
                .ToDictionary(x => x.Key,
                    kvp => kvp.Value
                        .Where(x => x.EndpointName == endpointName)
                        .ToDictionary(x => x.InstanceId ?? "$legacy", x => x.InputQueue));

            var policy = policyFactory();
            policy.Initialize(endpointName, queueMap);
            return policy;
        }

        public string GetDestinationQueueForEndpoint(string endpoint, DistributionPolicyContext context)
        {
            return endpointDistributionPolicies[endpoint].GetDestinationQueue(context);
        }

        public string GetDestinationQueueForRouter(string router, DistributionPolicyContext context)
        {
            return routerDistributionPolicies[router].GetDestinationQueue(context);
        }
    }
}