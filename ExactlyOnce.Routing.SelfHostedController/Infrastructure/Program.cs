using System;
using Azure.Storage.Blobs;
using ExactlyOnce.Routing.Controller.Infrastructure.CosmosDB;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using NServiceBus;

namespace ExactlyOnce.Routing.SelfHostedController
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var endpointUri = "https://localhost:8081";
            var primaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

            var cosmosClient = new CosmosClient(endpointUri, primaryKey);
            var outboxConfiguration = new OutboxConfiguration
            {
                DatabaseId = "RoutingTest",
                ContainerId = "Outbox",
                RetentionPeriod = TimeSpan.FromDays(30)
            };
            var outboxStore = new OutboxStore(cosmosClient, outboxConfiguration);
            var stateStore = new StateStore(cosmosClient, outboxConfiguration.DatabaseId);
            var routingTableSnapshotContainer = new BlobContainerClient("UseDevelopmentStorage=true", "routing-table");

            HostBuilderFactory.CreateHostBuilder<LearningTransport>("RoutingController", extensions => { }, outboxStore, stateStore, routingTableSnapshotContainer)
                .Build()
                .Run();
        }
    }
}