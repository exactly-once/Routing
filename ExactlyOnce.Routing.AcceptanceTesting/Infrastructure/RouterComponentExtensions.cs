using System;
using Azure.Storage.Blobs;
using ExactlyOnce.Router;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

public static class RouterComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithRouter<TContext>(this IScenarioWithEndpointBehavior<TContext> scenario, 
        string name, 
        string instanceId,
        Action<RouterConfiguration> configCallback)
        where TContext : ScenarioContext
    {
        return scenario.WithComponent(new RouterComponent(s =>
        {
            var cfg = new RouterConfiguration(name, instanceId, 
                new BlobContainerClient("UseDevelopmentStorage=true", "routing-table"), 
                "http://localhost:7071/api");
            configCallback(cfg);
            return cfg;
        }));
    }
}
