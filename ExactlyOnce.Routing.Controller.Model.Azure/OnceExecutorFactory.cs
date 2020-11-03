namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class OnceExecutorFactory
    {
        readonly ExactlyOnceProcessor processor;
        readonly Subscriptions subscriptions;

        public OnceExecutorFactory(ExactlyOnceProcessor processor, Subscriptions subscriptions)
        {
            this.processor = processor;
            this.subscriptions = subscriptions;
        }

        public IOnceExecutor<TState, TEntity> CreateGenericExecutor<TState, TEntity>(string requestId, string stateId) 
            where TState : EventDrivenState<TEntity>
        {
            return new OnceExecutor<TState, TEntity>(processor, subscriptions, requestId, stateId);
        }

        public IOnceEventProcessor CreateEventProcessor(string requestId, string stateId)
        {
            return new OnceEventProcessor(processor, subscriptions, requestId, stateId);
        }
    }
}