using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using Serilog;

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
        var hostId = Guid.Parse("E059FB33-3FD7-45F6-B06F-E0B83BDD91C7");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .CreateLogger();

        LogManager.Use<SerilogFactory>();

        Console.Title = "OtherEndpoint";

        var config = new EndpointConfiguration("OtherEndpoint");
        var transport = config.UseTransport<RabbitMQTransport>();
        transport.ConnectionString("host=localhost");
        transport.UseConventionalRoutingTopology();

        config.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(hostId);
        config.EnableInstallers();

        var routingSettings = config.UseExactlyOnceRouting(new BlobContainerClient("UseDevelopmentStorage=true", "routing-table"),
            "http://localhost:7071/api");

        routingSettings.ConnectToRouter("MyRouter");

        var endpoint = await Endpoint.Start(config);

        Console.WriteLine("Press <enter> to exit.");
        Console.ReadLine();

        await endpoint.Stop();
    }
}
