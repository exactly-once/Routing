using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace ExactlyOnce.Router
{
    class RouterImpl : IRouter
    {
        readonly Core.IRouter coreRouter;
        readonly RoutingTableManager routingTableManager;

        public RouterImpl(Core.IRouter coreRouter, RoutingTableManager routingTableManager)
        {
            this.coreRouter = coreRouter;
            this.routingTableManager = routingTableManager;
        }

        public async Task Start()
        {
            await coreRouter.Initialize().ConfigureAwait(false);

            var siteToQueueMap = coreRouter.Interfaces
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.TransportAddress);

            await routingTableManager.Start(siteToQueueMap).ConfigureAwait(false);
            await coreRouter.Start().ConfigureAwait(false);
        }

        public async Task Stop()
        {
            await coreRouter.Stop().ConfigureAwait(false);
            await routingTableManager.Stop().ConfigureAwait(false);
        }
    }
}