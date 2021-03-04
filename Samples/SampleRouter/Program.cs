using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ExactlyOnce.Router;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using Serilog;

namespace SampleRouter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var interfaces = args;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
                .CreateLogger();

            LogManager.Use<SerilogFactory>();

            Console.Title = "MyRouter";

            var config = new RouterConfiguration("SampleRouter", "A", new BlobContainerClient("UseDevelopmentStorage=true", "routing-table"),
                "http://localhost:7071/api");

            config.AutoCreateQueues();

            foreach (var i in interfaces)
            {
                var iface = config.AddInterface<TestTransport>(i, extensions =>
                {
                    extensions.BrokerName(i);
                });
            }

            var router = Router.Create(config);

            await router.Start().ConfigureAwait(false);

            Console.WriteLine("Press x to exit.");

            while (true)
            {
                try
                {
                    var key = Console.ReadKey();
                    if (key.Key == ConsoleKey.X)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            await router.Stop().ConfigureAwait(false);
        }
    }
}
