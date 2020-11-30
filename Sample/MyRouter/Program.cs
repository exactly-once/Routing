using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ExactlyOnce.Router;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using Serilog;

namespace MyRouter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
                .CreateLogger();

            LogManager.Use<SerilogFactory>();

            Console.Title = "MyRouter";

            var config = new RouterConfiguration("MyRouter", "A", new BlobContainerClient("UseDevelopmentStorage=true", "routing-table"),
                "http://localhost:7071/api");

            config.AutoCreateQueues();

            var siteA = config.AddInterface<LearningTransport>("SiteA", 
                t => { });
            var siteB = config.AddInterface<RabbitMQTransport>("SiteB", 
                t =>
                {
                    t.ConnectionString("host=localhost");
                    t.UseConventionalRoutingTopology();
                });

            var router = Router.Create(config);

            await router.Start().ConfigureAwait(false);

            Console.WriteLine("Press <enter> to exit.");
            Console.ReadLine();

            await router.Stop().ConfigureAwait(false);

        }
    }
}
