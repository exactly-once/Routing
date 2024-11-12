using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using Serilog;

namespace SampleInfrastructure
{
    public static class EndpointHelper
    {
        public static Task HostEndpoint(string endpointName, string[] args, Action<EndpointConfiguration> additionalConfig = null, Func<IEndpointInstance, Task> callback = null)
        {
            var rootCommand = new RootCommand
            {
                new Argument<string>("instanceId"),
                new Argument<string>("router"),
                new Argument<string>("brokerName")
            };

            rootCommand.Handler = CommandHandler.Create<InvocationContext, string, string, string>(
                async (context, instanceId, router, brokerName) =>
                {
                    var (config, routingSettings) = PrepareEndpoint(endpointName, instanceId, brokerName, router, additionalConfig);

                    var endpoint = await Endpoint.Start(config);

                    if (callback != null)
                    {
                        await callback(endpoint);
                    }
                    else
                    {
                        Console.WriteLine("Press <enter> to exit");
                        Console.ReadLine();
                    }

                    await endpoint.Stop();
                });

            var builder = new CommandLineBuilder(rootCommand);
            builder.UseDefaults();

            var parser = builder.Build();
            return parser.InvokeAsync(args);
        }

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

        public static (EndpointConfiguration, ExactlyOnceRoutingSettings) PrepareEndpoint(string endpointName, string instanceIdString, string brokerName, string router = null, Action<EndpointConfiguration> additionalConfig = null)
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

            additionalConfig?.Invoke(config);

            var routingSettings = config.UseExactlyOnceRouting(
                new BlobContainerClient("UseDevelopmentStorage=true", "routing-table"),
                "http://localhost:7071/api");

            routingSettings.ConnectToRouter(router ?? "SampleRouter");

            return (config, routingSettings);
        }
    }
}