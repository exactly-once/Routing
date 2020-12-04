using System;
using Azure.Storage.Blobs;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

public static class ManagedEndpointComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithManagedEndpoint<TContext, T>(this IScenarioWithEndpointBehavior<TContext> scenario,
        string instanceId,
        string routerAddress,
        Action<EndpointBehaviorBuilder<TContext>> behavior = null)
        where TContext : ScenarioContext
        where T : EndpointConfigurationBuilder
    {
        var configBuilder = Activator.CreateInstance<T>();
        var builder = new EndpointBehaviorBuilder<TContext>(configBuilder);
        builder.CustomConfig(cfg =>
        {
            cfg.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(DeterministicGuid.MakeId(instanceId));
            var routingSettings = cfg.UseExactlyOnceRouting(new BlobContainerClient("UseDevelopmentStorage=true", "routing-table"),
                "http://localhost:7071/api");

            routingSettings.ConnectToRouter(routerAddress);
        });
        behavior?.Invoke(builder);

        return scenario.WithComponent(builder.Build());
    }
}
