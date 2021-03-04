using System.Threading;
using System.Threading.Tasks;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public interface IOutboxStore
    {
        Task Initialize();
        Task<OutboxItem> Get(string stateId, string id, CancellationToken cancellationToken = default);
        Task Commit(string stateId, string transactionId, CancellationToken cancellationToken = default);
        Task Store(OutboxItem outboxItem, CancellationToken cancellationToken = default);
        Task Delete(string stateId, string itemId, CancellationToken cancellationToken = default);
    }
}