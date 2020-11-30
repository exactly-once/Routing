using System;
using System.Collections.Generic;

namespace ExactlyOnce.Routing.Endpoint.Model
{
    public class RouterRoutingTableLogic
    {
        readonly RoutingTable routingTable;
        readonly DistributionPolicy distributionPolicy;

        public RouterRoutingTableLogic(RoutingTable routingTable, Dictionary<string, Func<IDistributionPolicy>> distributionPolicyFactories)
        {
            this.routingTable = routingTable;
            distributionPolicy = new DistributionPolicy(routingTable, distributionPolicyFactories);
        }

        public (RoutingSlip, string) GetNextHop(string incomingSite, string destinationHandler, string destinationEndpoint, string destinationSite, Dictionary<string, string> messageHeaders)
        {
            if (!routingTable.DestinationSiteToNextHopMapping.TryGetValue(incomingSite, out var destinationsFromIncomingInterface))
            {
                throw new MoveToDeadLetterQueueException($"The routing table does not contain information for site {incomingSite}");
            }

            if (!destinationsFromIncomingInterface.TryGetValue(destinationSite, out var destination))
            {
                throw new MoveToDeadLetterQueueException($"Site {destinationSite} hosting endpoint {destinationEndpoint} is not reachable from {incomingSite}.");
            }

            if (destination.NextHopSite == null)
            {
                //Route to destination endpoint

                var distributionPolicyContext = new DistributionPolicyContext(destinationSite, messageHeaders);
                var nextHopQueue = distributionPolicy.GetDestinationQueueForEndpoint(destinationEndpoint, distributionPolicyContext);

                return (new RoutingSlip(destinationHandler, destinationEndpoint, destinationSite, nextHopQueue), destinationSite);
            }
            else
            {
                var distributionPolicyContext = new DistributionPolicyContext(destination.NextHopSite, messageHeaders);

                var nextHopQueue = distributionPolicy.GetDestinationQueueForRouter(destination.Router, distributionPolicyContext);
                return (new RoutingSlip(destinationHandler, destinationEndpoint, destinationSite, nextHopQueue), destination.NextHopSite);
            }
        }
    }
}