using System.Linq;
using Azure.Storage.Blobs;
using ExactlyOnce.Routing.NServiceBus;
using NServiceBus.Features;
using NServiceBus.Hosting;
using NServiceBus.Routing;
using NServiceBus.Transport;
using NServiceBus.Unicast;

// Features are always defined in NServiceBus namespace
// ReSharper disable once CheckNamespace
namespace NServiceBus
{
    using System;

    class ExactlyOnceRoutingFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var transportInfrastructure = context.Settings.Get<TransportInfrastructure>();

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

                return new RoutingTableManager(settings.ControllerUrl, settings.ControllerContainerClient, settings.RouterName, settings.SiteName, context.Settings.EndpointName(), hostInfo.HostId.ToString(), messageKindMap, messageHandlersMap);
            }, DependencyLifecycle.SingleInstance);
            context.RegisterStartupTask(b => b.Build<RoutingTableManager>());

            context.Container.ConfigureComponent(b => new RoutingLogic(b.Build<IRoutingTable>(),
                context.Settings.Get<EndpointInstances>(),
                context.Settings.Get<DistributionPolicy>(),
                instance => transportInfrastructure.ToTransportAddress(LogicalAddress.CreateRemoteAddress(instance))), DependencyLifecycle.SingleInstance);

            context.Pipeline.Replace("UnicastPublishRouterConnector", b => new PublishRoutingConnector(b.Build<RoutingLogic>()));
            context.Pipeline.Replace("MulticastPublishRouterBehavior", b => new PublishRoutingConnector(b.Build<RoutingLogic>()));
            context.Pipeline.Replace("UnicastSendRouterConnector", b => new SendRoutingConnector(b.Build<RoutingLogic>()));

            context.Pipeline.Replace("MessageDrivenSubscribeTerminator", new NullSubscribeTerminator(), "handles subscribe operations");
            context.Pipeline.Replace("MessageDrivenUnsubscribeTerminator", new NullUnsubscribeTerminator(), "handles unsubscribe operations");

            context.Pipeline.Register(b => new ReroutingBehavior(b.Build<RoutingLogic>()), "Reroutes lost messages to their correct destinations");
        }

        static string BuildHandlerName(Type handlerType)
        {
            return $"{handlerType.FullName}, {handlerType.Assembly.GetName().Name}";
        }
    }
}
