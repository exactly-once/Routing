using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public interface IOnceExecutor<TState, TEntity> 
        where TState : State<TEntity>
    {
        Task<EventMessage[]> Once(Func<TEntity, IEnumerable<IEvent>> action, Func<TEntity> constructor);
    }

    class OnceExecutor<TState, TEntity> : IOnceExecutor<TState, TEntity>
        where TState : State<TEntity>
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

        public async Task<EventMessage[]> Once(
            Func<TEntity, IEnumerable<IEvent>> action,
            Func<TEntity> constructor)
        {
            var maxDelay = TimeSpan.FromSeconds(20);
            var delay = TimeSpan.FromMilliseconds(500);

            do
            {
                try
                {
                    var operationId = $"{typeof(TState).Name}-{stateId}-{requestId}";

                    var entityKey = DeterministicGuid.MakeId(stateId).ToString();

                    var result = await processor.Process(operationId, entityKey, typeof(TState), state =>
                    {
                        var eventDrivenState = (State<TEntity>) state;
                        var events = eventDrivenState.Invoke(action, constructor, subscriptions).ToArray();
                        return events;
                    });

                    return result;
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