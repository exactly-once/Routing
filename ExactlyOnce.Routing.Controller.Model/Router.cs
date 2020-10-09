using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class Router
    {
        public Router(int changeSequence, string name, Dictionary<string, RouterInstance> instances, List<string> interfacesToSites, List<RouterPendingChanges> pendingChanges)
        {
            ChangeSequence = changeSequence;
            Name = name;
            Instances = instances;
            InterfacesToSites = interfacesToSites;
            PendingChanges = pendingChanges;
        }

        public string Name { get; }
        public int ChangeSequence { get; private set; }
        public List<string> InterfacesToSites { get; private set; }
        public Dictionary<string, RouterInstance> Instances { get;  }
        public List<RouterPendingChanges> PendingChanges { get; }

        public void TruncateChanges(int processedVersion)
        {
            PendingChanges.RemoveAll(x => x.Sequence <= processedVersion);
        }

        public bool ProcessReport(RouterInstanceReport report)
        {
            var newInstance = new RouterInstance(report.InstanceId, report.SiteInterfaces);
            Instances[report.InstanceId] = newInstance;

            var uniqueSites = Instances.Values.SelectMany(x => x.InterfacesToSites).Distinct();

            //Only site interfaces supported by all instances of the router are reported
            var updatedSiteInterfaces = uniqueSites.Where(x => Instances.Values.All(i => i.InterfacesToSites.Contains(x)))
                .ToList();

            var interfacesAdded = updatedSiteInterfaces.Except(InterfacesToSites).ToList();
            var interfacesRemoved = InterfacesToSites.Except(updatedSiteInterfaces).ToList();

            InterfacesToSites = updatedSiteInterfaces;

            if (interfacesRemoved.Any() || interfacesAdded.Any())
            {
                ChangeSequence++;
                PendingChanges.Add(new RouterPendingChanges(ChangeSequence, interfacesAdded, interfacesRemoved));
                return true;
            }

            return false;

        }

        /*
         *How do endpoints know which site they belong to?
         * - The router knows site names for its interfaces. The endpoint sends a hello message to its designated router and the router reports to which site the endpoint belongs
         * - The endpoint knows its site name and reports it to the controller. The router reports the sites for its interfaces.
         *
         * The problem with the first approach is that the routing can't be established until the hello message is received by the router.
         * The advantage of the first approach is that the convention can be used for the router name e.g. "ExactlyOnce.DefaultGateway" so that the endpoints don't have to have any
         * configuration as long as the default router queue name is used.
         *
         */

    }
}