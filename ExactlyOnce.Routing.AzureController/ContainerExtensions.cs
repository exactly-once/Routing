using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace ExactlyOnce.Routing.AzureController
{
    public static class ContainerExtensions
    {
        public static Task<ItemResponse<T>> ReadObjectAsync<T>(this Container container,
            string id,
            ItemRequestOptions requestOptions = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var itemId = $"{typeof(T).Name}_{id}";

            return container.ReadItemAsync<T>(itemId, PartitionKey.None /*TODO*/, requestOptions, cancellationToken);
        }
    }
}