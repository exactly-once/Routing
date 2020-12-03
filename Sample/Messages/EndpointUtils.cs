using System;
using Azure.Storage.Blobs;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using Serilog;

public class EndpointUtils
{
    public static EndpointConfiguration PrepareEndpoint(string name, string[] args)
    {
        var instanceId = args.Length > 1
            ? args[0]
            : "a";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .CreateLogger();

        LogManager.Use<SerilogFactory>();

        Console.Title = name;

        var config = new EndpointConfiguration(name);

        if (args.Length < 2)
        {
            config.UseTransport<LearningTransport>();
        }
        else
        {
            if (string.Equals(args[1], "learning", StringComparison.OrdinalIgnoreCase))
            {
                var transport = config.UseTransport<LearningTransport>();
            }
            if (string.Equals(args[1], "rabbit", StringComparison.OrdinalIgnoreCase))
            {
                var transport = config.UseTransport<RabbitMQTransport>();
                transport.ConnectionString("host=localhost");
                transport.UseConventionalRoutingTopology();
            }
        }

        config.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(DeterministicGuid.MakeId(instanceId));
        config.EnableInstallers();

        var routingSettings = config.UseExactlyOnceRouting(new BlobContainerClient("UseDevelopmentStorage=true", "routing-table"),
            "http://localhost:7071/api");
        //"https://localhost:44378/");

        routingSettings.ConnectToRouter("MyRouter");

        return config;
    }
}