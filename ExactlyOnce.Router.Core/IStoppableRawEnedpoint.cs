using System.Threading.Tasks;

namespace ExactlyOnce.Router.Core
{
    /// <summary>
    /// Represents an endpoint in the shutdown phase.
    /// </summary>
    public interface IStoppableRawEndpoint
    {
        /// <summary>
        /// Stops the endpoint.
        /// </summary>
        Task Stop();
    }
}