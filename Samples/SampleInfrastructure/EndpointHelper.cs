using System;
using Azure.Storage.Blobs;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using Serilog;

namespace SampleInfrastructure
{
    public static class EndpointHelper
    {
        public static (EndpointConfiguration, RoutingSettings<NServiceBus.TestTransport>) PrepareLegacyEndpoint(string endpointName, string instanceIdString, string brokerName)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
                .CreateLogger();

            LogManager.Use<SerilogFactory>();

            Console.Title = endpointName;

            var config = new EndpointConfiguration(endpointName);

            var transport = config.UseTransport<NServiceBus.TestTransport>();
            transport.BrokerName(brokerName);

            config.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(DeterministicGuid.MakeId(instanceIdString));
            config.EnableInstallers();

            return (config, transport.Routing());
        }

        public static (EndpointConfiguration, ExactlyOnceRoutingSettings) PrepareEndpoint(string endpointName, string instanceIdString, string brokerName)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
                .CreateLogger();

            LogManager.Use<SerilogFactory>();

            Console.Title = endpointName;

            var config = new EndpointConfiguration(endpointName);

            var transport = config.UseTransport<NServiceBus.TestTransport>();
            transport.BrokerName(brokerName);

            config.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(DeterministicGuid.MakeId(instanceIdString));
            config.EnableInstallers();

            var routingSettings = config.UseExactlyOnceRouting(
                new BlobContainerClient("UseDevelopmentStorage=true", "routing-table"),
                "http://localhost:7071/api");

            routingSettings.ConnectToRouter("SampleRouter");

            return (config, routingSettings);
        }
    }
}