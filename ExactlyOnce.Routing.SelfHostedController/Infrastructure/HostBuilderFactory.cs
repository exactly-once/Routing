using System;
using System.Linq;
using Azure.Storage.Blobs;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using NServiceBus.Transport;

namespace ExactlyOnce.Routing.SelfHostedController
{
    public static class HostBuilderFactory
    {
        public static IHostBuilder CreateHostBuilder<T>(
            string queueName,
            Action<TransportExtensions<T>> transportCustomization,
            IOutboxStore outboxStore, 
            IStateStore stateStore,
            BlobContainerClient routingTableSnapshotContainer)
            where T : TransportDefinition, new()
        {
            return Host.CreateDefaultBuilder(new string[0])
                .ConfigureServices(collection =>
                {
                    var subscriptions = new Subscriptions();
                    subscriptions.Subscribe<RouteTableStorageHandler, RoutingTableChanged>(e => "");
                    subscriptions.Subscribe<SignalRHandler, RoutingTableChanged>(e => "");
                    collection.AddSingleton(new OnceExecutorFactory(new ExactlyOnceProcessor(outboxStore, stateStore), subscriptions));
                    collection.AddSingleton(stateStore);
                    collection.AddSingleton(sp =>
                        new NServiceBusRawHostedService<T>(queueName, transportCustomization, async () =>
                        {
                            await outboxStore.Initialize().ConfigureAwait(false);
                            await stateStore.Initialize().ConfigureAwait(false);

                        }, sp.GetService<EventLoopHandler>(), sp.GetServices<IMessageHandler>().ToArray()));

                    collection.AddSingleton(sp => sp.GetService<NServiceBusRawHostedService<T>>().Sender);
                    collection.AddSingleton<IHostedService>(sp => sp.GetService<NServiceBusRawHostedService<T>>());
                    collection.AddSingleton<EventLoopHandler>();
                    collection.AddSingleton<IMessageHandler, SignalRHandler>();
                    collection.AddSingleton<IMessageHandler, RouteTableStorageHandler>(sp => new RouteTableStorageHandler(routingTableSnapshotContainer));
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:7071/");
                });

        }
    }
}
