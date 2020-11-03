using System;
using ExactlyOnce.Routing.AzureController;
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
                    o.DatabaseId = "E1Sandbox";
                    o.ContainerId = "Outbox";
                    o.RetentionPeriod = TimeSpan.FromSeconds(30);
                });

                c.UseCosmosClient(() =>
                {
                    var endpointUri = Environment.GetEnvironmentVariable("E1_CosmosDB_EndpointUri");
                    var primaryKey = Environment.GetEnvironmentVariable("E1_CosmosDB_Key");

                    return new CosmosClient(endpointUri, primaryKey);
                });
            });
        }
    }
}