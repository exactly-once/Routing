using System;
using ExactlyOnce.Routing.AzureController;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(HostStartup))]

namespace ExactlyOnce.Routing.AzureController
{
    public class HostStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddExactlyOnce(c =>
            {
                c.ConfigureOutbox(o =>
                {
                    o.DatabaseId = "RoutingTest";
                    o.ContainerId = "Outbox";
                    o.RetentionPeriod = TimeSpan.FromDays(30);
                });

                c.Subscribe<MessageRoutingState, MessageHandlerAdded>(e => e.HandledMessageType);
                c.Subscribe<MessageRoutingState, MessageHandlerRemoved>(e => e.HandledMessageType);
                c.Subscribe<MessageRoutingState, MessageTypeAdded>(e => e.FullName);
                c.Subscribe<MessageRoutingState, MessageKindChanged>(e => e.FullName);
                c.Subscribe<RoutingTableState, MessageRoutingChanged>(e => "Instance");
                c.Subscribe<RoutingTableState, RouteChanged>(e => "Instance");
                c.Subscribe<RoutingTableState, EndpointInstanceLocationUpdated>(e => "Instance");

                c.Subscribe<NotificationApi, RoutingTableChanged>(e => "");

                c.Subscribe<RouteTableStorageApi, RoutingTableChanged>(e => "");

                c.UseCosmosClient(() =>
                {
                    var endpointUri = "https://localhost:8081";
                    var primaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

                    return new CosmosClient(endpointUri, primaryKey);
                });
            });
        }
    }
}