using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Client;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

public static class ControllerComponentExtensions
{
    public static IScenarioWithEndpointBehavior<TContext> WithController<TContext>(this IScenarioWithEndpointBehavior<TContext> scenario)
        where TContext : ScenarioContext
    {
        return scenario.WithComponent(new ControllerComponent());
    }
}

public static class SequenceExtensions
{
    public static SequenceBuilder<TContext> Do<TContext>(this IScenarioWithEndpointBehavior<TContext> endpointBehavior, string step, 
        Func<TContext, RoutingControllerClient, Task<bool>> handler)
        where TContext : ScenarioContext, ISequenceContext
    {
        return new SequenceBuilder<TContext>(endpointBehavior, step, handler);
    }
}

public class SequenceBuilder<TContext>
    where TContext : ScenarioContext, ISequenceContext
{
    public SequenceBuilder(IScenarioWithEndpointBehavior<TContext> endpointBehavior, string step, Func<TContext, RoutingControllerClient, Task<bool>> handler)
    {
        this.endpointBehavior = endpointBehavior;
        sequence.Do(step, handler);
    }

    public SequenceBuilder<TContext> Do(string step, Func<TContext, RoutingControllerClient, Task<bool>> handler)
    {
        sequence.Do(step, handler);
        return this;
    }

    public SequenceBuilder<TContext> Do(string step, Func<TContext, RoutingControllerClient, Task> handler)
    {
        sequence.Do(step, handler);
        return this;
    }

    public IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> doneCriteria = null)
    {
        var behavior = new SequenceComponent<TContext>((context, client) => sequence.Continue(context, client));
        return endpointBehavior.WithComponent(behavior).Done(ctx => sequence.IsFinished(ctx) && (doneCriteria == null || doneCriteria(ctx)));
    }

    IScenarioWithEndpointBehavior<TContext> endpointBehavior;
    Sequence<TContext> sequence = new Sequence<TContext>();
}

public class SequenceComponent<TContext> : IComponentBehavior
        where TContext : ScenarioContext
{
    public SequenceComponent(Func<TContext, RoutingControllerClient, Task<bool>> checkDone)
    {
        this.checkDone = checkDone;
    }

    public bool Done
    {
        get
        {
            exceptionInfo?.Throw();
            return isDone;
        }
    }

    public Task<ComponentRunner> CreateRunner(RunDescriptor run)
    {
        return Task.FromResult<ComponentRunner>(new Runner(checkDone, () => isDone = true, info => exceptionInfo = info, (TContext)run.ScenarioContext));
    }

    Func<TContext, RoutingControllerClient, Task<bool>> checkDone;
    volatile ExceptionDispatchInfo exceptionInfo;
    volatile bool isDone;

    class Runner : ComponentRunner
    {
        public Runner(
            Func<TContext, RoutingControllerClient, Task<bool>> isDone, 
            Action setDone, 
            Action<ExceptionDispatchInfo> setException, 
            TContext scenarioContext)
        {
            this.setException = setException;
            this.isDone = isDone;
            this.setDone = setDone;
            this.scenarioContext = scenarioContext;
            controllerClient = new RoutingControllerClient("http://localhost:7071/api");
        }

        public override string Name => "SequenceController";

        public override Task Start(CancellationToken token) => Task.FromResult(0);

        public override Task ComponentsStarted(CancellationToken token)
        {
            tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            checkTask = Task.Run(async () =>
            {
                try
                {
                    while (!tokenSource.IsCancellationRequested)
                    {
                        if (await isDone(scenarioContext, controllerClient).ConfigureAwait(false))
                        {
                            setDone();
                            return;
                        }

                        await Task.Delay(100, tokenSource.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    setException(ExceptionDispatchInfo.Capture(e));
                }
            }, tokenSource.Token);
            return Task.FromResult(0);
        }

        public override async Task Stop()
        {
            if (checkTask == null)
            {
                return;
            }

            tokenSource.Cancel();
            try
            {
                await checkTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                //Swallow
            }
            finally
            {
                tokenSource.Dispose();
            }
        }

        Func<TContext, RoutingControllerClient, Task<bool>> isDone;
        Action setDone;
        TContext scenarioContext;
        Task checkTask;
        CancellationTokenSource tokenSource;
        Action<ExceptionDispatchInfo> setException;
        RoutingControllerClient controllerClient;
    }
}

public interface ISequenceContext
{
    int Step { get; set; }
}

class Sequence<TContext>
    where TContext : ISequenceContext
{
    public Sequence<TContext> Do(string step, Func<TContext, RoutingControllerClient, Task<bool>> handler)
    {
        steps.Add(handler);
        stepNames.Add(step);
        return this;
    }

    public Sequence<TContext> Do(string step, Func<TContext, RoutingControllerClient, Task> handler)
    {
        steps.Add(async (context, client) =>
        {
            await handler(context, client).ConfigureAwait(false);
            return true;
        });
        stepNames.Add(step);
        return this;
    }

    public bool IsFinished(TContext context)
    {
        return context.Step >= steps.Count;
    }

    public async Task<bool> Continue(TContext context, RoutingControllerClient client)
    {
        var currentStep = context.Step;
        if (currentStep >= steps.Count)
        {
            return true;
        }

        var step = steps[currentStep];
        var advance = await step(context, client).ConfigureAwait(false);
        if (advance)
        {
            var nextStep = currentStep + 1;
            var finished = nextStep >= steps.Count;
            if (finished)
            {
                Console.WriteLine("Sequence finished");
            }
            else
            {
                Console.WriteLine($"Advancing from {stepNames[currentStep]} to {stepNames[nextStep]}");
            }

            context.Step = nextStep;
            return finished;
        }

        return false;
    }

    List<Func<TContext, RoutingControllerClient, Task<bool>>> steps = new List<Func<TContext, RoutingControllerClient, Task<bool>>>();
    List<string> stepNames = new List<string>();
}