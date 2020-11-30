
namespace ExactlyOnce.Router
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
            var routingTableManager = new RoutingTableManager(
                config.ControllerUrl, 
                config.ControllerContainerClient,
                config.DistributionPolicyConfiguration,
                config.RouterConfig.Name,
                config.InstanceId);

            config.RoutingLogic.Initialize(routingTableManager);

            return new RouterImpl(Core.Router.Create(config.RouterConfig), routingTableManager);
        }
    }
}