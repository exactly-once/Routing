using System.Threading.Tasks;

namespace ExactlyOnce.Router
{
    /// <summary>
    /// An instance of a router
    /// </summary>
    public interface IRouter
    {
        /// <summary>
        /// Initializes and starts the router.
        /// </summary>
        Task Start();

        /// <summary>
        /// Stops the router.
        /// </summary>
        Task Stop();
    }
}