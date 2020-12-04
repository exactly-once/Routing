using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ExactlyOnce.Routing.Controller.Infrastructure.CosmosDB;
using ExactlyOnce.Routing.Controller.Model.Azure;
using ExactlyOnce.Routing.SelfHostedController;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

class ControllerComponent : IComponentBehavior
{
    public async Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        return new Runner();
    }

    class Runner : ComponentRunner
    {
        IHost host;
        public override string Name => "Controller";

        public override Task Start(CancellationToken token)
        {
            var endpointUri = "https://localhost:8081";
            var primaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

            var cosmosClient = new CosmosClient(endpointUri, primaryKey);
            var outboxConfiguration = new OutboxConfiguration
            {
                DatabaseId = "RoutingAcceptanceTests",
                ContainerId = "Outbox",
                RetentionPeriod = TimeSpan.FromDays(30)
            };
            var outboxStore = new OutboxStore(cosmosClient, outboxConfiguration);
            var stateStore = new StateStore(cosmosClient, outboxConfiguration.DatabaseId);
            var routingTableSnapshotContainer = new BlobContainerClient("UseDevelopmentStorage=true", "routing-table");

            host = HostBuilderFactory.CreateHostBuilder<TestTransport>("RoutingController", extensions =>
                {
                    extensions.BrokerOmega();
                }, outboxStore,
                    stateStore, routingTableSnapshotContainer).Build();
            
            return host.StartAsync(token);
        }

        public override async Task Stop()
        {
            if (host != null)
            {
                await host.StopAsync();
            }
        }
    }
}