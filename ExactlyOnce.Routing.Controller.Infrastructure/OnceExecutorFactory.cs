namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class OnceExecutorFactory
    {
        readonly ExactlyOnceProcessor processor;
        readonly Subscriptions subscriptions;
        readonly Search search;

        public OnceExecutorFactory(ExactlyOnceProcessor processor, Subscriptions subscriptions, Search search)
        {
            this.processor = processor;
            this.subscriptions = subscriptions;
            this.search = search;
        }

        public IOnceExecutor<TState, TEntity> CreateGenericExecutor<TState, TEntity>(string requestId, string stateId) 
            where TState : State<TEntity>
            where TEntity : new()
        {
            return new OnceExecutor<TState, TEntity>(processor, subscriptions, search, requestId, stateId);
        }

        public IOnceEventProcessor CreateEventProcessor(string requestId, string stateId)
        {
            return new OnceEventProcessor(processor, subscriptions, requestId, stateId);
        }
    }
}