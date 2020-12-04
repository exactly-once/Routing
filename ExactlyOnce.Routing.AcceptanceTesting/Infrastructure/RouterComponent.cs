using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.Router;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

class RouterComponent : IComponentBehavior
{
    Func<ScenarioContext, RouterConfiguration> configCallback;

    public RouterComponent(Func<ScenarioContext, RouterConfiguration> config)
    {
        this.configCallback = config;
    }

    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        var config = configCallback(run.ScenarioContext);
        config.AutoCreateQueues();
        var router = Router.Create(config);
        return Task.FromResult<ComponentRunner>(new Runner(router, "Router"));
    }

    class Runner : ComponentRunner
    {
        readonly IRouter router;

        public Runner(IRouter router, string name)
        {
            this.router = router;
            Name = name;
        }

        public override Task Start(CancellationToken token)
        {
            return router.Start();
        }

        public override Task ComponentsStarted(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public override Task Stop()
        {
            return router != null 
                ? router.Stop() 
                : Task.CompletedTask;
        }

        public override string Name { get; }
    }
}