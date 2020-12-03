using System.Threading;
using System.Threading.Tasks;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public interface IOutboxStore
    {
        Task Initialize();
        Task<OutboxItem> Get(string id, CancellationToken cancellationToken = default);
        Task Commit(string transactionId, CancellationToken cancellationToken = default);
        Task Store(OutboxItem outboxItem, CancellationToken cancellationToken = default);
        Task Delete(string itemId, CancellationToken cancellationToken = default);
    }
}