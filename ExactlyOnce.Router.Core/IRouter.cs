using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExactlyOnce.Router.Core
{
    /// <summary>
    /// An instance of a router
    /// </summary>
    public interface IRouter
    {
        /// <summary>
        /// Initializes the router.
        /// </summary>
        /// <returns></returns>
        Task Initialize();

        /// <summary>
        /// Initializes and starts the router.
        /// </summary>
        Task Start();

        /// <summary>
        /// Gets the collection of interfaces of the router.
        /// </summary>
        IReadOnlyDictionary<string, IRawEndpoint> Interfaces { get; }

        /// <summary>
        /// Stops the router.
        /// </summary>
        Task Stop();
    }
}