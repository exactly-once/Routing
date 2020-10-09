using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RoutingData
    {
        public RoutingData(Dictionary<string, MessageKind> messageTypes, Dictionary<string, MessageRouting> messageRouting, Dictionary<string, EndpointSiteRoutingPolicy> endpointSiteRoutingPolicy)
        {
            MessageTypes = messageTypes;
            MessageRouting = messageRouting;
            EndpointSiteRoutingPolicy = endpointSiteRoutingPolicy;
        }
        public Dictionary<string, EndpointSiteRoutingPolicy> EndpointSiteRoutingPolicy { get; }
        public Dictionary<string, MessageKind> MessageTypes { get; }
        public Dictionary<string, MessageRouting> MessageRouting { get; }

        public void ConfigureEndpointSiteMapping(string endpoint, EndpointSiteRoutingPolicy policy)
        {
            EndpointSiteRoutingPolicy[endpoint] = policy;
        }

        public void Subscribe(string messageType, string handlerType, string endpoint)
        {
            GetRouting(messageType).Subscribe(handlerType, endpoint);
        }
        
        public void Unsubscribe(string messageType, string handlerType, string endpoint)
        {
            GetRouting(messageType).Unsubscribe(handlerType, endpoint);
        }

        public void Appoint(string messageType, string handlerType, string endpoint)
        {
            GetRouting(messageType).Appoint(handlerType, endpoint);
        }

        public void Dismiss(string messageType, string handlerType, string endpoint)
        {
            GetRouting(messageType).Dismiss(handlerType, endpoint);
        }

        MessageRouting GetRouting(string messageType)
        {
            if (!MessageRouting.TryGetValue(messageType, out var routing))
            {
                throw new Exception($"Unrecognized message type {messageType}");
            }

            return routing;
        }

        public void UpdateEndpoint(string endpoint, EndpointPendingChanges changeSet)
        {
            //NEXT:
            /*
             * Each endpoint reports the recognized message types with their kinds. Currently majority wins, but we might
             * change it so that the endpoint reports "conflict" if not all instances agree.
             *
             * Then RoutingData aggregates the data from the endpoint to derive the message kind to be used for the routing.
             * The majority rule here might not be enough.
             *
             * The routing should only be allowed to change it all endpoints agree on the kind
             * If kind is changed then the routing should be shown as invalid. This may happen if an event is changed to a command
             * and it has more than one destination. In that case only "Dismiss" action should be available
             *
             */

            //Process recognized messages

            //Destination handler can be activated only if present in a single instance
            //If handler is added to a different instance while it is active, the newly added endpoint is not active
            //Users can switch activation to a different endpoint

            foreach (var handler in changeSet.MessageHandlersAdded)
            {
                if (!MessageTypes.ContainsKey(handler.HandledMessage))
                {
                    throw new Exception(
                        $"Unable to process endpoint update message. Message type {handler.HandledMessage} handled by handler {handler.Name} is not recognized.");
                }

                if (!MessageRouting.TryGetValue(handler.HandledMessage, out var messageRouting))
                {
                    messageRouting = new MessageRouting(handler.HandledMessage, new List<Destination>());
                    MessageRouting[handler.HandledMessage] = messageRouting;
                }

                messageRouting.HandlerAdded(handler.Name, handler.Site, endpoint, MessageKind.Undefined);
            }

            foreach (var handler in changeSet.MessageHandlersRemoved)
            {
                if (!MessageRouting.TryGetValue(handler.HandledMessage, out var messageRouting))
                {
                    //NOOP? Log warning? We should not see that a handler has been removed if we previously had no information about it
                    return;
                }

                messageRouting.HandlerRemoved(handler.Name, handler.Site, endpoint);
            }
        }

        /*
             * Problem:
             *  - Row to route replies? We would like to be as explicit as possible.
             *
             * Solution:
             *  - Use a pipeline behavior to attach a ReplyToHandler header that contains the type of the handler that has sent the message
             *  - The reply is router to that handler, wherever it is.
             *  - One can override it via SendOptions i.e. RouteReplyToHandler(x) to cause the reply to be routed to a different handler
             *  - Problem: sending from outside of a handler
             *    - Solution: require explicit RouteReplyToHandler on these calls
             */
    }
}