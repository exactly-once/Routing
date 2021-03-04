using System;
using System.Threading.Tasks;
using NServiceBus;
using SampleInfrastructure;

namespace SenderReceiverRoutingMigration.Receiver
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var (config, _) = EndpointHelper.PrepareLegacyEndpoint("SenderReceiverRoutingMigration.Receiver", "a", "Alpha");

            //var (config, routing) = EndpointHelper.PrepareEndpoint("SenderReceiverRoutingMigration.Receiver", "a", "Alpha");
            //routing.EnableLegacyMigrationMode();

            var endpoint = await Endpoint.Start(config);

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

            await endpoint.Stop();
        }
    }
}
