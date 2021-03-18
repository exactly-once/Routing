
using System;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model.Azure;

namespace ExactlyOnce.Routing.Controller.Infrastructure.SQL
{
    public class OutboxStore : IOutboxStore
    {
        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public Task<OutboxItem> Get(string stateId, string id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Commit(string stateId, string transactionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Store(OutboxItem outboxItem, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Delete(string stateId, string itemId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
