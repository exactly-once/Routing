using System.Linq;
using ExactlyOnce.Routing.ApiContract;
using ExactlyOnce.Routing.Client;
using ExactlyOnce.Routing.NServiceBus;
using NServiceBus.Features;
using NServiceBus.Hosting;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NServiceBus.Unicast;
using NServiceBus.Unicast.Messages;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

// Features are always defined in NServiceBus namespace
// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    using System;

    class ExactlyOnceRoutingFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.TryGet("NServiceBus.Subscriptions.EnableMigrationMode", out bool enabled) && enabled)
            {
                throw new Exception("ExactlyOnce routing is not compatible with subscription migration mode.");
            }

            var conventions = context.Settings.Get<Conventions>();

            var settings = context.Settings.Get<ExactlyOnceRoutingSettings>();

            if (settings.RouterName == null && settings.SiteName == null)
            {
                throw new Exception("When using ExactlyOnce Routing you need to specify either name of a local router or name of the site.");
            }

            if (settings.RouterName != null && settings.SiteName != null)
            {
                throw new Exception("When using ExactlyOnce Routing you cannot specify both name of a local router or name of the site.");
            }

            var transportInfra = context.Settings.Get<TransportInfrastructure>();
            var distributionPolicy = context.Settings.Get<DistributionPolicy>();
            var endpointInstances = context.Settings.Get<EndpointInstances>();

            var routingControllerClient = new RoutingControllerClient(settings.ControllerUrl);

            var legacyMigrationEnabled = settings.MigrationSettings != null;
            LegacyRoutingLogic legacyRoutingLogic = null;

            if (legacyMigrationEnabled)
            {
                legacyRoutingLogic = new LegacyRoutingLogic(
                    routingControllerClient,
                    context.Settings.EndpointName(),
                    settings.MigrationSettings.LegacyDestinations,
                    distributionPolicy,
                    endpointInstances,
                    x => transportInfra.ToTransportAddress(LogicalAddress.CreateRemoteAddress(x)));
            }

            context.Container.ConfigureComponent(b =>
            {
                var handlerRegistry = b.Build<MessageHandlerRegistry>();
                var hostInfo = b.Build<HostInformation>();

                var messageTypesRecognized = handlerRegistry.GetMessageTypes()
                    .Where(t => !conventions.IsInSystemConventionList(t))
                    .Where(t => !t.IsInterface) //Do not support interface events (yet)
                    .Where(t => t.BaseType == typeof(object)) //Do not support message inheritance
                    .ToArray();

                var allHandlers = messageTypesRecognized
                    .SelectMany(x => handlerRegistry.GetHandlersFor(x))
                    .Distinct();

                var messageHandlersMap = allHandlers
                    .ToDictionary(x => BuildHandlerName(x.HandlerType), x =>
                    {
                        var handlerInterfaces = x.HandlerType.GetInterfaces()
                            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));

                        var handledMessages = handlerInterfaces.Select(i => i.GetGenericArguments()[0]).ToArray();

                        if (handledMessages.Length > 1)
                        {
                            throw new Exception($"Handlers that handle multiple message types are not supported. In order to use ExactlyOnce.Routing you need to split {x.HandlerType.FullName} into separate handlers");
                        }

                        return handledMessages[0].FullName;
                    });

                var messageKindMap = messageTypesRecognized.ToDictionary(x => x.FullName,
                    x =>
                    {
                        if (conventions.IsEventType(x))
                        {
                            return MessageKind.Event;
                        }

                        if (conventions.IsCommandType(x))
                        {
                            return MessageKind.Command;
                        }

                        return MessageKind.Message;
                    });

                return new RoutingTableManager(settings.ControllerUrl,
                    routingControllerClient,
                    settings.ControllerContainerClient, 
                    settings.SiteRoutingPolicyConfiguration, 
                    settings.DistributionPolicyConfiguration, 
                    settings.RouterName, 
                    settings.SiteName, 
                    context.Settings.EndpointName(), 
                    context.Settings.LocalAddress(),
                    hostInfo.HostId.ToString(), 
                    messageKindMap, 
                    messageHandlersMap,
                    settings.EnableLegacyMigrationMode().LegacyDestinations,
                    b.Build<IDispatchMessages>(),
                    x => legacyRoutingLogic?.SetSite(x),
                    legacyMigrationEnabled);
            }, DependencyLifecycle.SingleInstance);
            context.RegisterStartupTask(b => b.Build<RoutingTableManager>());

            context.Container.ConfigureComponent(b => new RoutingLogic(b.Build<IRoutingTable>()), DependencyLifecycle.SingleInstance);

            if (legacyMigrationEnabled)
            {
                if (transportInfra.OutboundRoutingPolicy.Publishes == OutboundRoutingType.Multicast)
                {
                    context.Pipeline.Replace("MulticastPublishRouterBehavior", b => new LegacyNativePublishRoutingConnector(b.Build<RoutingLogic>()));
                }
                else
                {
                    context.Pipeline.Replace("UnicastPublishRouterConnector", b => new LegacyStorageDrivenPublishRoutingConnector(b.Build<RoutingLogic>(), b.Build<ISubscriptionStorage>(), b.Build<MessageMetadataRegistry>()));
                }
            }
            else
            {
                context.Pipeline.Replace("UnicastPublishRouterConnector", b => new PublishRoutingConnector(b.Build<RoutingLogic>()));
                context.Pipeline.Replace("MulticastPublishRouterBehavior", b => new PublishRoutingConnector(b.Build<RoutingLogic>()));
            }

            context.Pipeline.Replace("UnicastSendRouterConnector", b => new SendRoutingConnector(b.Build<RoutingLogic>(), legacyRoutingLogic));

            context.Pipeline.Replace("MessageDrivenSubscribeTerminator", new NullSubscribeTerminator(), "handles subscribe operations");
            context.Pipeline.Replace("MessageDrivenUnsubscribeTerminator", new NullUnsubscribeTerminator(), "handles unsubscribe operations");

            context.Pipeline.Register(b => new ReroutingBehavior(b.Build<RoutingLogic>()), "Reroutes lost messages to their correct destinations");

            context.Pipeline.Register(new DualRoutingDeduplicationBehavior(context.Settings.EndpointName()), "Discards duplicate dual routing messages");

            context.Pipeline.Replace("RoutingToDispatchConnector", new RoutingToDispatchConnector());
        }

        static string BuildHandlerName(Type handlerType)
        {
            return $"{handlerType.FullName}, {handlerType.Assembly.GetName().Name}";
        }
    }
}
