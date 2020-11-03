using System.Collections.Generic;
using System.Linq;
using ExactlyOnce.Routing.Controller.Model;
using NUnit.Framework;

namespace ExactlyOnce.Routing.Tests
{
    [TestFixture]
    public class TopologyTests
    {
        [Test]
        public void Can_route_between_two_sites()
        {
            var topology = new Topology(new Dictionary<string, RouterInfo>(),new List<RouterInterfaceRule>());

            var events = topology.UpdateRouter("Router-1", new List<string> {"A", "B"}).ToArray();

            var destinationMapChanged = events.OfType<DestinationSiteToNextHopMapChanged>().Single();
            var topologyChanged = events.OfType<TopologyChanged>().Single();

            Assert.IsTrue(topologyChanged.Connections.Any(c => c.SourceSite == "A" && c.DestinationSite == "B"));
            Assert.IsTrue(topologyChanged.Connections.Any(c => c.SourceSite == "B" && c.DestinationSite == "A"));
        }

        [Test]
        public void Can_route_via_intermediary_site()
        {
            var topology = new Topology(new Dictionary<string, RouterInfo>(), new List<RouterInterfaceRule>());

            var events = topology.UpdateRouter("Router-1", new List<string> { "A", "B" }).ToArray();
            events = topology.UpdateRouter("Router-2", new List<string> { "B", "C" }).ToArray();

            var destinationMapChanged = events.OfType<DestinationSiteToNextHopMapChanged>().Single();
            var topologyChanged = events.OfType<TopologyChanged>().Single();

            Assert.IsTrue(topologyChanged.Connections.Any(c => c.SourceSite == "A" && c.DestinationSite == "B"));
            Assert.IsTrue(topologyChanged.Connections.Any(c => c.SourceSite == "B" && c.DestinationSite == "C"));

            AssertIsReachable(destinationMapChanged, "A", "C", "B");
            AssertIsReachable(destinationMapChanged, "C", "A", "B");
        }

        [Test]
        public void Uses_shortest_path()
        {
            var topology = new Topology(new Dictionary<string, RouterInfo>(), new List<RouterInterfaceRule>());

            var events = topology.UpdateRouter("Router-1", new List<string> { "A", "B", "C" }).ToArray();
            events = topology.UpdateRouter("Router-2", new List<string> { "B", "C" }).ToArray();

            var destinationMapChanged = events.OfType<DestinationSiteToNextHopMapChanged>().Single();
            var topologyChanged = events.OfType<TopologyChanged>().Single();

            Assert.IsTrue(topologyChanged.Connections.Any(c => c.SourceSite == "A" && c.DestinationSite == "B"));
            Assert.IsTrue(topologyChanged.Connections.Any(c => c.SourceSite == "B" && c.DestinationSite == "C"));

            AssertIsReachable(destinationMapChanged, "A", "C", "C");
            AssertIsReachable(destinationMapChanged, "C", "A", "A");
        }

        void AssertIsReachable(DestinationSiteToNextHopMapChanged e, string origin, string destination, string via)
        {
            if (!e.DestinationSiteToNextHopMap.TryGetValue(origin, out var destinations))
            {
                Assert.Fail($"Origin not known: {origin}");
            }

            if (!destinations.TryGetValue(destination, out var siteInfo))
            {
                Assert.Fail($"Destination {destination} not reachable from {origin}.");
            }

            if (siteInfo.NextHopSite != via)
            {
                Assert.Fail($"The designated route from {origin} to {destination} is expected to be {via}, not {siteInfo.NextHopSite}");
            }
        }
    }
}