using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using NServiceBus;

class MyMessage : ICommand
{
}

class MyHandler : IHandleMessages<MyMessage>
{
    public Task Handle(MyMessage message, IMessageHandlerContext context)
    {
        Console.WriteLine("Message received.");
        return Task.CompletedTask;
    }
}

namespace Orders
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var hostId = Guid.Parse("E059FB33-3FD7-45F6-B06F-E0B83BDD91C7");

            var config = new EndpointConfiguration("SomeEndpoint");
            config.UseTransport<LearningTransport>();
            config.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(hostId);

            var routingSettings = config.UseExactlyOnceRouting(new BlobContainerClient("UseDevelopmentStorage=true", "routing-table"), 
                "http://localhost:7071/api");

            routingSettings.SetSiteName("SiteA");

            var endpoint = await Endpoint.Start(config);

            Console.WriteLine("Press <enter> to send a message.");
            while (true)
            {
                Console.ReadLine();
                try
                {
                    await endpoint.Send(new MyMessage()).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            //await endpoint.Stop();
        }
    }
}
