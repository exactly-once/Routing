using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Controller.Model
{
    /*
     * Endpoint -> MessageRouting -> RoutingTable
     * Endpoint -> MessagesViewModel
     * Router -> Topology -> RoutingTable
     */

    public class RouteAdded : IEvent
    {
        public RouteAdded(string messageType, string handlerType, string endpoint, List<string> sites)
        {
            MessageType = messageType;
            HandlerType = handlerType;
            Endpoint = endpoint;
            Sites = sites;
        }

        public string MessageType { get; }
        public string HandlerType { get; }
        public string Endpoint { get; }
        public List<string> Sites { get; }
    }

    public class RouteChanged : IEvent
    {
        public RouteChanged(string messageType, string handlerType, string endpoint, List<string> sites)
        {
            MessageType = messageType;
            HandlerType = handlerType;
            Endpoint = endpoint;
            Sites = sites;
        }

        public string MessageType { get; }
        public string HandlerType { get; }
        public string Endpoint { get; }
        public List<string> Sites { get; }
    }

    public class RouteRemoved : IEvent
    {
        public RouteRemoved(string messageType, string handlerType, string endpoint)
        {
            MessageType = messageType;
            HandlerType = handlerType;
            Endpoint = endpoint;
        }

        public string MessageType { get; }
        public string HandlerType { get; }
        public string Endpoint { get; }
    }

    public class MessageRouting : IEventHandler<MessageHandlerAdded>,
        IEventHandler<MessageHandlerRemoved>,
        IEventHandler<MessageKindChanged>
    {
        public MessageRouting(string messageType, List<Destination> destinations)
        {
            MessageType = messageType;
            Destinations = destinations;
        }

        public string MessageType { get; }
        public List<Destination> Destinations { get; }

        public IEnumerable<IEvent> Subscribe(string handlerType, string endpoint)
        {
            var subscriber = Destinations.SingleOrDefault(x => x.Handler == handlerType && x.Endpoint == endpoint);
            if (subscriber == null)
            {
                throw new Exception($"No handler {handlerType} in endpoint {endpoint}.");
            }

            if (subscriber.MessageKind == MessageKind.Undefined)
            {
                throw new Exception($"Cannot subscribe {handlerType} at {endpoint} to {MessageType} because instances of this endpoint don't agree on the kind of this message.");
            }

            if (subscriber.State != DestinationState.Inactive)
            {
                throw new Exception($"Handler {handlerType} hosted in {endpoint} is already subscribed to {MessageType}.");
            }

            if (subscriber.MessageKind == MessageKind.Command)
            {
                throw new Exception($"Cannot subscribe {handlerType} at {endpoint} to {MessageType} because this message type is considered a command by the handler.");
            }

            //If a given handler is already subscribed in another endpoint, unsubscribe the other handler and subscribe this one
            //This is equivalent of moving the handler from one endpoint to another
            var subscriberInAnotherEndpoint = Destinations.SingleOrDefault(x => x.State != DestinationState.Inactive && x.Handler == handlerType && x.Endpoint != endpoint);
            if (subscriberInAnotherEndpoint != null)
            {
                DeactivateOrRemove(subscriberInAnotherEndpoint);
                yield return new RouteRemoved(MessageType, subscriberInAnotherEndpoint.Handler, subscriberInAnotherEndpoint.Endpoint);
            }

            subscriber.Activate();
            yield return new RouteAdded(MessageType, subscriber.Handler, subscriber.Endpoint, subscriber.Sites);
        }

        void DeactivateOrRemove(Destination destination)
        {
            if (destination.Deactivate())
            {
                Destinations.Remove(destination);
            }
        }

        public IEnumerable<IEvent> Unsubscribe(string handlerType, string endpoint)
        {
            var subscriber = Destinations.SingleOrDefault(x => x.Handler == handlerType && x.Endpoint == endpoint);
            if (subscriber == null)
            {
                throw new Exception($"No handler {handlerType} in endpoint {endpoint}.");
            }

            if (subscriber.MessageKind == MessageKind.Undefined)
            {
                throw new Exception($"Cannot unsubscribe {handlerType} at {endpoint} from {MessageType} because instances of this endpoint don't agree on the kind of this message.");
            }

            if (subscriber.State == DestinationState.Inactive)
            {
                throw new Exception($"Handler {handlerType} hosted in {endpoint} is not subscribed to {MessageType}.");
            }

            if (subscriber.MessageKind == MessageKind.Command)
            {
                throw new Exception($"Cannot subscribe {handlerType} at {endpoint} from {MessageType} because this message type is considered a command by the handler.");
            }

            DeactivateOrRemove(subscriber);
            yield return new RouteRemoved(MessageType, subscriber.Handler, subscriber.Endpoint);
        }

        public IEnumerable<IEvent> Appoint(string handlerType, string endpoint)
        {
            var handler = Destinations.SingleOrDefault(x => x.Handler == handlerType && x.Endpoint == endpoint);
            if (handler == null)
            {
                throw new Exception($"No handler {handlerType} in endpoint {endpoint}.");
            }

            if (handler.MessageKind == MessageKind.Undefined)
            {
                throw new Exception($"Cannot set {handlerType} at {endpoint} as destination for {MessageType} because instances of this endpoint don't agree on the kind of this message.");
            }

            if (handler.State != DestinationState.Inactive)
            {
                throw new Exception($"Handler {handlerType} hosted in {endpoint} is already set as a destination for message {MessageType}.");
            }

            if (handler.MessageKind == MessageKind.Event)
            {
                throw new Exception($"Cannot set {handlerType} at {endpoint} as destination for {MessageType} because this message type is considered an event by the handler.");
            }

            //Deactivate all other routes. There can be only one command for commands
            foreach (var destination in Destinations.Where(x => x.Handler != handlerType))
            {
                DeactivateOrRemove(destination);
                yield return new RouteRemoved(MessageType, destination.Handler, destination.Endpoint);
            }

            //If a given handler is already activated, switch to different endpoint
            //This is equivalent of moving the handler from one endpoint to another
            var handlerInAnotherEndpoint = Destinations.SingleOrDefault(x => x.State != DestinationState.Inactive && x.Handler == handlerType && x.Endpoint != endpoint);
            if (handlerInAnotherEndpoint != null)
            {
                DeactivateOrRemove(handlerInAnotherEndpoint);
                yield return new RouteRemoved(MessageType, handlerInAnotherEndpoint.Handler, handlerInAnotherEndpoint.Endpoint);
            }

            handler.Activate();
            yield return new RouteAdded(MessageType, handler.Handler, handler.Endpoint, handler.Sites);
        }

        public IEnumerable<IEvent> Dismiss(string handlerType, string endpoint)
        {
            var handler = Destinations.SingleOrDefault(x => x.Handler == handlerType && x.Endpoint == endpoint);
            if (handler == null)
            {
                throw new Exception($"No handler {handlerType} in endpoint {endpoint}.");
            }

            if (handler.MessageKind == MessageKind.Undefined)
            {
                throw new Exception($"Cannot dismiss {handlerType} at {endpoint} as destination for {MessageType} because instances of this endpoint don't agree on the kind of this message.");
            }

            if (handler.State == DestinationState.Inactive)
            {
                throw new Exception($"Handler {handlerType} hosted in {endpoint} is not set as destination for message {MessageType}.");
            }

            if (handler.MessageKind == MessageKind.Event)
            {
                throw new Exception($"Cannot dismiss {handlerType} at {endpoint} as destination for {MessageType} because this message type is considered an event by the handler.");
            }

            DeactivateOrRemove(handler);
            yield return new RouteRemoved(MessageType, handler.Handler, handler.Endpoint);
        }

        public void MessageKindChanged(string endpoint, MessageKind messageKind)
        {
            foreach (var destination in Destinations.Where(x => x.Endpoint == endpoint))
            {
                destination.MessageKindChanged(messageKind);
            }
        }

        public IEnumerable<IEvent> HandlerAdded(string handlerType, string handlerSite, string endpoint, MessageKind messageKind)
        {
            var destination = Destinations.FirstOrDefault(x => x.Handler == handlerType && x.Endpoint == endpoint);
            if (destination == null)
            {
                destination = new Destination(handlerType, endpoint, DestinationState.Inactive, messageKind, new List<string> { handlerSite });
                Destinations.Add(destination);
            }
            else
            {
                destination.HandlerAdded(handlerSite);
                if (destination.State != DestinationState.Inactive)
                {
                    yield return new RouteChanged(MessageType, destination.Handler, destination.Endpoint,
                        destination.Sites);
                }
            }
        }

        public IEnumerable<IEvent> HandlerRemoved(string handlerType, string handlerSite, string endpoint)
        {
            var destination = Destinations.FirstOrDefault(x => x.Handler == handlerType && x.Endpoint == endpoint);
            if (destination == null)
            {
                throw new Exception($"Handler {handlerType} hosted in endpoint {endpoint} has not been added");
            }

            /*
             * If we remove a handler from a site when the route is active, we marked this site as a dead end
             *  - but only if there is no other router for this message type that is active in the target site
             *
             * We ensure that for a command type there may be a single active route in each site.
             * Message can be re-routed if they contain information required to
             * Some site routes can be disabled e.g. no route between "sibling" sites
             * Some site routes may require explicit routing information (site name)
             */

            if (destination.HandlerRemoved(handlerSite))
            {
                Destinations.Remove(destination);
            }
            else
            {
                if (destination.State != DestinationState.Inactive)
                {
                    yield return new RouteChanged(MessageType, destination.Handler, destination.Endpoint,
                        destination.Sites);
                }
            }
        }

        public IEnumerable<IEvent> Handle(MessageHandlerAdded e)
        {
            return HandlerAdded(e.HandlerType, e.Site, e.Endpoint, MessageKind.Undefined /*TODO WHY?*/);
        }

        public IEnumerable<IEvent> Handle(MessageHandlerRemoved e)
        {
            return HandlerRemoved(e.HandlerType, e.Site, e.Endpoint);
        }

        public IEnumerable<IEvent> Handle(MessageKindChanged e)
        {
            MessageKindChanged(e.Endpoint, e.NewKind);
            return Enumerable.Empty<IEvent>();
        }
    }
}