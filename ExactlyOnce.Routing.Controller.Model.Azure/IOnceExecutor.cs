using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public interface IOnceExecutor<TState, TEntity> 
        where TState : EventDrivenState<TEntity>
    {
        Task<EventMessage[]> Once(Func<TEntity, IEnumerable<IEvent>> action);
    }

    class OnceExecutor<TState, TEntity> : IOnceExecutor<TState, TEntity>
        where TState : EventDrivenState<TEntity>
    {
        readonly ExactlyOnceProcessor processor;
        readonly Subscriptions subscriptions;
        readonly string requestId;
        readonly string stateId;

        public OnceExecutor(ExactlyOnceProcessor processor, Subscriptions subscriptions, string requestId, string stateId)
        {
            this.processor = processor;
            this.subscriptions = subscriptions;
            this.requestId = requestId;
            this.stateId = stateId;
        }

        public async Task<EventMessage[]> Once(Func<TEntity, IEnumerable<IEvent>> action)
        {
            var maxDelay = TimeSpan.FromSeconds(20);
            var delay = TimeSpan.FromMilliseconds(500);

            do
            {
                try
                {
                    var operationId = $"{typeof(TState).Name}-{stateId}-{requestId}";

                    await processor.Process(operationId, stateId, typeof(TState), state =>
                    {
                        var eventDrivenState = (EventDrivenState<TEntity>) state;
                        var result = eventDrivenState.Invoke(action, subscriptions).ToArray();
                        return result;
                    });
                }
                catch (OptimisticConcurrencyFailure)
                {
                    await Task.Delay(delay);

                    delay *= 2;
                }
            } while (delay <= maxDelay);

            throw new OptimisticConcurrencyFailure();

        }
    }
}