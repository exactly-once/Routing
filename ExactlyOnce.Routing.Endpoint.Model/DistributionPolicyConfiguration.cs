using System;
using System.Collections.Generic;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class DistributionPolicyConfiguration
    {
        readonly Dictionary<string, Func<IDistributionPolicy>> distributionPolicies = new Dictionary<string, Func<IDistributionPolicy>>()
        {
            {"default", () => new RoundRobinDistributionPolicy()},
            {"round-robin", () => new RoundRobinDistributionPolicy()}
        };

        public void AddDistributionPolicy(string name, Func<IDistributionPolicy> policyFactory)
        {
            distributionPolicies.Add(name, policyFactory);
        }

        public DistributionPolicy CreateDistributionPolicy(RoutingTable routingTable)
        {
            return new DistributionPolicy(routingTable, distributionPolicies);
        }
    }
}