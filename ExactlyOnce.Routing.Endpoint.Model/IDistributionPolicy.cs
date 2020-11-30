using System.Collections.Generic;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public interface IDistributionPolicy
    {
        void Initialize(string endpointName, Dictionary<string, Dictionary<string, string>> siteToInstanceToQueueMap);
        string GetDestinationQueue(DistributionPolicyContext context);
    }
}