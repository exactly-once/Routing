using System.Threading.Tasks;

namespace ExactlyOnce.Router.Core
{
    /// <summary>
    /// Represents an endpoint in the running phase.
    /// </summary>
    public interface IReceivingRawEndpoint : IStoppableRawEndpoint, IRawEndpoint
    {
        /// <summary>
        /// Stops receiving of messages. The endpoint can still send messages.
        /// </summary>
        Task<IStoppableRawEndpoint> StopReceiving();
    }
}