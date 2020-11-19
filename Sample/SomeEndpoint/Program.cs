using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using NServiceBus;

class MyMessageHandler : IHandleMessages<MyMessage>
{
    public Task Handle(MyMessage message, IMessageHandlerContext context)
    {
        Console.WriteLine("Message received.");
        return Task.CompletedTask;
    }
}

class MyEventHandler : IHandleMessages<MyEvent>
{
    public Task Handle(MyEvent message, IMessageHandlerContext context)
    {
        Console.WriteLine("Event received.");
        return Task.CompletedTask;
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var hostId = Guid.Parse("4A6598E8-2860-4726-B6A1-3EB7BDAB5F7F");

        var config = new EndpointConfiguration("SomeEndpoint");
        config.UseTransport<LearningTransport>();
        config.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(hostId);

        var routingSettings = config.UseExactlyOnceRouting(new BlobContainerClient("UseDevelopmentStorage=true", "routing-table"),
            "http://localhost:7071/api");

        routingSettings.SetSiteName("SiteA");

        var endpoint = await Endpoint.Start(config);

        Console.WriteLine("Press <enter> to exit.");
        Console.ReadLine();

        await endpoint.Stop();
    }
}
