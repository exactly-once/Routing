using System;
using System.Linq;
using System.Threading.Tasks;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public interface IOnceEventProcessor
    {
        Task<EventMessage[]> Once(EventMessage eventMessage);
    }

    public class OnceEventProcessor : IOnceEventProcessor
    {
        readonly ExactlyOnceProcessor processor;
        readonly Subscriptions subscriptions;
        readonly Search search;
        readonly string requestId;
        readonly string stateId;

        public OnceEventProcessor(ExactlyOnceProcessor processor, Subscriptions subscriptions, Search search,
            string requestId, string stateId)
        {
            this.processor = processor;
            this.subscriptions = subscriptions;
            this.search = search;
            this.requestId = requestId;
            this.stateId = stateId;
        }

        public async Task<EventMessage[]> Once(EventMessage eventMessage)
        {
            var maxDelay = TimeSpan.FromSeconds(20);
            var delay = TimeSpan.FromMilliseconds(500);

            var destinationType = typeof(OnceEventProcessor).Assembly.GetType(eventMessage.DestinationType, true);

            do
            {
                try
                {
                    var operationId = $"{destinationType.Name}-{stateId}-{requestId}";

                    var entityKey = DeterministicGuid.MakeId(stateId).ToString();

                    return await processor.Process(operationId, entityKey, destinationType, state =>
                    {
                        var eventDrivenState = (State)state;
                        var events = eventDrivenState.OnEvent(eventMessage, subscriptions, search).ToArray();
                        return events;
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