using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using NServiceBus;

class Program
{
    static async Task Main(string[] args)
    {
        var hostId = Guid.Parse("8C26D86E-8715-493D-8AFA-98747CB6BAFC");

        var config = new EndpointConfiguration("Sender");
        config.UseTransport<LearningTransport>();
        config.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(hostId);

        var routingSettings = config.UseExactlyOnceRouting(new BlobContainerClient("UseDevelopmentStorage=true", "routing-table"),
            "http://localhost:7071/api");

        routingSettings.SetSiteName("SiteA");

        var endpoint = await Endpoint.Start(config);

        Console.WriteLine("Press c to send a command or e to publish an event.");
        while (true)
        {
            try
            {
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.C:
                        await endpoint.Send(new MyMessage()).ConfigureAwait(false);
                        break;
                    case ConsoleKey.E:
                        await endpoint.Publish(new MyEvent()).ConfigureAwait(false);
                        break;
                    case ConsoleKey.X:
                        await endpoint.Stop();
                        Environment.Exit(0);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
