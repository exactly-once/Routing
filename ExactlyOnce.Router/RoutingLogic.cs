using System.Threading.Tasks;
using ExactlyOnce.Router.Core;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace ExactlyOnce.Router
{
    class RoutingLogic
    {
        RoutingTableManager routingTableManager;

        public void Initialize(RoutingTableManager routingTableManager)
        {
            this.routingTableManager = routingTableManager;
        }

        public Task HandleMessage(IMessageRoutingContext context)
        {
            var headers = context.ReceivedMessage.Headers;
            if (headers.TryGetValue("ExactlyOnce.Routing.ControlMessage.Type", out var controlMessageType))
            {
                if (controlMessageType == "Hello")
                {
                    return HandleEndpointHello(context);
                }

                throw new UnforwardableMessageException($"Control message type {controlMessageType} not recognized {context.ReceivedMessage.MessageId}.");
            }

            return ForwardMessage(context);
        }

        Task ForwardMessage(IMessageRoutingContext context)
        {
            var message = context.ReceivedMessage;
            var headers = message.Headers;

            if (!headers.TryGetValue("ExactlyOnce.Routing.DestinationEndpoint", out var destinationEndpoint))
            {
                throw new UnforwardableMessageException(
                    $"Missing destination endpoint information on message {message.MessageId}.");
            }

            if (!headers.TryGetValue("ExactlyOnce.Routing.DestinationHandler", out var destinationHandler))
            {
                throw new UnforwardableMessageException(
                    $"Missing destination handler information on message {message.MessageId}.");
            }

            if (!headers.TryGetValue("ExactlyOnce.Routing.DestinationSite", out var destinationSite))
            {
                throw new UnforwardableMessageException(
                    $"Missing destination site information on message {message.MessageId}.");
            }

            var (routingSlip, nextHopSite) = routingTableManager.GetNextHop(context.IncomingInterface, destinationHandler,
                destinationEndpoint,
                destinationSite, headers);

            var forwardInterface = context.GetOutgoingInterface(nextHopSite);

            var outgoingMessage = new OutgoingMessage(message.MessageId, headers, message.Body);
            var operations = new TransportOperations(new TransportOperation(outgoingMessage, new UnicastAddressTag(routingSlip.NextHopQueue)));
            return forwardInterface.Dispatch(operations, message.TransportTransaction, message.Extensions);
        }

        Task HandleEndpointHello(IMessageRoutingContext context)
        {
            var headers = context.ReceivedMessage.Headers;
            if (!headers.TryGetValue("ExactlyOnce.Routing.ControlMessage.Hello.EndpointName", out var endpointName))
            {
                throw new UnforwardableMessageException(
                    $"Missing endpoint name value on Hello message {context.ReceivedMessage.MessageId}.");
            }

            if (!headers.TryGetValue("ExactlyOnce.Routing.ControlMessage.Hello.InstanceId", out var instanceId))
            {
                throw new UnforwardableMessageException(
                    $"Missing instance id value on Hello message {context.ReceivedMessage.MessageId}.");
            }

            return routingTableManager.SendHelloToController(context.ReceivedMessage.MessageId, endpointName, instanceId,
                context.IncomingInterface);
        }
    }
}