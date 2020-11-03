using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class Router
    {
        public Router(string name)
            : this(name, new Dictionary<string, RouterInstance>(), new List<string>())
        {
        }

        public Router(string name, Dictionary<string, RouterInstance> instances, List<string> interfacesToSites)
        {
            Name = name;
            Instances = instances;
            InterfacesToSites = interfacesToSites;
        }

        public string Name { get; }
        public List<string> InterfacesToSites { get; private set; }
        public Dictionary<string, RouterInstance> Instances { get;  }

        public IEnumerable<IEvent> OnRouterStartup(string instanceId, List<string> siteInterfaces)
        {
            var newRouter = !Instances.Any();
            var newInstance = new RouterInstance(instanceId, siteInterfaces);
            Instances[instanceId] = newInstance;

            var uniqueSites = Instances.Values.SelectMany(x => x.InterfacesToSites).Distinct();

            //Only site interfaces supported by all instances of the router are reported
            var updatedSiteInterfaces = uniqueSites
                .Where(x => Instances.Values.All(i => i.InterfacesToSites.Contains(x)))
                .OrderBy(x => x)
                .ToList();

            if (!updatedSiteInterfaces.SequenceEqual(InterfacesToSites))
            {
                if (newRouter)
                {
                    yield return new RouterAdded(Name, updatedSiteInterfaces);
                }
                else
                {
                    yield return new RouterInterfacesChanged(Name, updatedSiteInterfaces);
                }
            }

            InterfacesToSites = updatedSiteInterfaces;
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