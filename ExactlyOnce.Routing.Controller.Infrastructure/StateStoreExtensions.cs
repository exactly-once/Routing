using System.Threading;
using System.Threading.Tasks;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public static class StateStoreExtensions
    {
        public static async Task<(T, object)> Load<T>(this IStateStore stateStore, string stateId, CancellationToken cancellationToken = default)
            where T : State
        {
            var (state, etag) = await stateStore.Load(stateId, typeof(T), cancellationToken).ConfigureAwait(false);
            return ((T) state, etag);
        }
    }
}