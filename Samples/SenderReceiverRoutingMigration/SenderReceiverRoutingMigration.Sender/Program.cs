using System;
using System.Threading.Tasks;
using NServiceBus;
using SampleInfrastructure;
using SenderReceiverRoutingMigration.Shared;

namespace SenderReceiverRoutingMigration.Sender
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var (config, routing) = EndpointHelper.PrepareLegacyEndpoint("SenderReceiverRoutingMigration.Sender", "a", "Alpha");
            routing.RouteToEndpoint(typeof(MyCommand), "SenderReceiverRoutingMigration.Receiver");

            //var (config, routing) = EndpointHelper.PrepareEndpoint("Sender", "a", "Alpha");
            //var migrationMode = routing.EnableLegacyMigrationMode();
            //migrationMode.RouteToEndpoint(typeof(MyCommand), "SenderReceiverRoutingMigration.Receiver");

            var endpoint = await Endpoint.Start(config);

            Console.WriteLine("Press c to send a command or x to exit.");

            while (true)
            {
                try
                {
                    var key = Console.ReadKey();
                    if (key.Key == ConsoleKey.C)
                    {
                        await endpoint.Send(new MyCommand());
                    }
                    else if (key.Key == ConsoleKey.X)
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
