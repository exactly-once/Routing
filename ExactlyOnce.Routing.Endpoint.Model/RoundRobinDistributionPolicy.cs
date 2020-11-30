using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RoundRobinDistributionPolicy : IDistributionPolicy
    {
        Dictionary<string, List<string>> queueMap;
        string endpoint;
        int roundRobinValue;

        public void Initialize(string endpointName, Dictionary<string, Dictionary<string, string>> siteToInstanceToQueueMap)
        {
            endpoint = endpointName;
            queueMap = siteToInstanceToQueueMap.ToDictionary(kvp => kvp.Key,
                kvp => kvp.Value.Select(x => x.Value).ToList());
        }

        public string GetDestinationQueue(DistributionPolicyContext context)
        {
            if (!queueMap.TryGetValue(context.DestinationSite, out var queues))
            {
                throw new Exception($"Selected destination site {context.DestinationSite} contains no instances of endpoint {endpoint}. Routing table is inconsistent. Contact support.");
            }
            var value = Interlocked.Increment(ref roundRobinValue);
            return queues[value % queues.Count];
        }
    }
}