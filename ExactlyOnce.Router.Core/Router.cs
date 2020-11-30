using System.Linq;

namespace ExactlyOnce.Router.Core
{
    /// <summary>
    /// Allows creating routers.
    /// </summary>
    public static class Router
    {
        /// <summary>
        /// Creates a new instance of a router based on the provided configuration.
        /// </summary>
        /// <param name="config">Router configuration.</param>
        public static IRouter Create(RouterConfiguration config)
        {
            var typeGenerator = new RuntimeTypeGenerator();

            var sendOnlyInterfaces = config.SendOnlyInterfaceFactories.Select(x => x()).ToArray();
            var interfaces = config.InterfaceFactories.Select(x => x()).ToArray();

            return new RouterImpl(config.Name, interfaces, sendOnlyInterfaces, typeGenerator);
        }
    }
}