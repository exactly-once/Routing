﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageRouting : IEventHandler<MessageHandlerAdded>,
        IEventHandler<MessageHandlerRemoved>,
        IEventHandler<MessageKindChanged>,
        IEventHandler<MessageTypeAdded>,
        IEventHandler<LegacyDestinationAdded>
    {
        const string LegacyHandlerType = "$legacy";

        [JsonConstructor]
        public MessageRouting(string messageType, List<Destination> destinations)
        {
            MessageType = messageType;
            Destinations = destinations ?? new List<Destination>();
        }

        public MessageRouting()
        {
            Destinations = new List<Destination>();
        }

        public string MessageType { get; private set; }
        public List<Destination> Destinations { get; }

        public IEnumerable<IEvent> Subscribe(string handlerType, string endpoint, string replacedHandlerType)
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

            var removed = new List<RouteRemoved>();

            if (replacedHandlerType != null)
            {
                var replacedSubscriber = Destinations.SingleOrDefault(x =>
                    x.Handler == replacedHandlerType && x.State == DestinationState.Active);
                if (replacedSubscriber == null)
                {
                    throw new Exception($"Cannot replace subscription of {replacedHandlerType} with {handlerType} at {endpoint} to {MessageType} because the handler to be replaced is not subscribed.");
                }
                DeactivateOrRemove(replacedSubscriber);
                removed.Add(new RouteRemoved(MessageType, replacedSubscriber.Handler, replacedSubscriber.Endpoint));
            }

            //If a given handler is already subscribed in another endpoint, unsubscribe the other handler and subscribe this one
            //This is equivalent of moving the handler from one endpoint to another
            var subscriberInAnotherEndpoint = Destinations.SingleOrDefault(x => x.State != DestinationState.Inactive && x.Handler == handlerType && x.Endpoint != endpoint);
            if (subscriberInAnotherEndpoint != null)
            {
                DeactivateOrRemove(subscriberInAnotherEndpoint);
                removed.Add(new RouteRemoved(MessageType, subscriberInAnotherEndpoint.Handler, subscriberInAnotherEndpoint.Endpoint));
            }

            subscriber.Activate();
            var routeAdded = new RouteAdded(MessageType, subscriber.Handler, subscriber.Endpoint, subscriber.Sites);

            yield return new MessageRoutingChanged(routeAdded, removed);
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

            yield return new MessageRoutingChanged(new List<RouteRemoved>
            {
                new RouteRemoved(MessageType, subscriber.Handler, subscriber.Endpoint)
            });
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

            var removed = new List<RouteRemoved>();

            //Deactivate all other routes. There can be only one command for commands
            foreach (var destination in Destinations.Where(x => x.Handler != handlerType && x.State != DestinationState.Inactive))
            {
                DeactivateOrRemove(destination);
                removed.Add(new RouteRemoved(MessageType, destination.Handler, destination.Endpoint));
            }

            //If a given handler is already activated, switch to different endpoint
            //This is equivalent of moving the handler from one endpoint to another
            var handlerInAnotherEndpoint = Destinations.SingleOrDefault(x => x.State != DestinationState.Inactive && x.Handler == handlerType && x.Endpoint != endpoint);
            if (handlerInAnotherEndpoint != null)
            {
                DeactivateOrRemove(handlerInAnotherEndpoint);
                removed.Add(new RouteRemoved(MessageType, handlerInAnotherEndpoint.Handler, handlerInAnotherEndpoint.Endpoint));
            }

            handler.Activate();
            var routeAdded = new RouteAdded(MessageType, handler.Handler, handler.Endpoint, handler.Sites);

            yield return new MessageRoutingChanged(routeAdded, removed);
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

            yield return new MessageRoutingChanged(new List<RouteRemoved>
            {
                new RouteRemoved(MessageType, handler.Handler, handler.Endpoint)
            });
        }

        public IEnumerable<IEvent> HandlerAdded(string handlerType, string handlerSite, string endpoint, MessageKind messageKind)
        {
            var legacyDestination = Destinations.FirstOrDefault(x => x.Handler == LegacyHandlerType && x.Endpoint == endpoint);
            if (legacyDestination != null)
            {
                if (legacyDestination.State != DestinationState.Active)
                {
                    Destinations.Remove(legacyDestination);
                    var destination = new Destination(handlerType, endpoint, DestinationState.Inactive, messageKind, new List<string> { handlerSite });
                    Destinations.Add(destination);
                }
                else
                {
                    legacyDestination.Migrate(handlerSite, handlerType);
                    yield return new RouteChanged(MessageType, LegacyHandlerType, legacyDestination.Endpoint, legacyDestination.Sites, handlerType);
                }
            }
            else
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
                            destination.Sites, destination.Handler);
                    }
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
                        destination.Sites, destination.Handler);
                }
            }
        }

        public IEnumerable<IEvent> Handle(MessageHandlerAdded e)
        {
            if (e.AutoSubscribe)
            {
                return HandlerAdded(e.HandlerType, e.Site, e.Endpoint, e.MessageKind)
                    .Concat(Subscribe(e.HandlerType, e.Endpoint, null));
            }

            return HandlerAdded(e.HandlerType, e.Site, e.Endpoint, e.MessageKind);
        }

        public IEnumerable<IEvent> Handle(MessageHandlerRemoved e)
        {
            return HandlerRemoved(e.HandlerType, e.Site, e.Endpoint);
        }

        public IEnumerable<IEvent> Handle(MessageKindChanged e)
        {
            foreach (var destination in Destinations.Where(x => x.Endpoint == e.Endpoint))
            {
                destination.MessageKindChanged(e.NewKind);
            }

            return Enumerable.Empty<IEvent>();
        }

        public IEnumerable<IEvent> Handle(MessageTypeAdded e)
        {
            MessageType = e.FullName;

            return Enumerable.Empty<IEvent>();
        }

        public IEnumerable<IEvent> Handle(LegacyDestinationAdded e)
        {
            MessageType = e.HandledMessageType;

            var legacyDestination = Destinations.FirstOrDefault(x => x.Handler == LegacyHandlerType && x.Endpoint == e.Endpoint);
            if (legacyDestination != null)
            {
                legacyDestination.HandlerAdded(e.Site);
                if (legacyDestination.State != DestinationState.Inactive)
                {
                    yield return new RouteChanged(MessageType, legacyDestination.Handler, legacyDestination.Endpoint,
                        legacyDestination.Sites, legacyDestination.Handler);
                }
            }
            else
            {
                var destination = Destinations.FirstOrDefault(x => x.Endpoint == e.Endpoint);
                if (destination != null)
                {
                    var existingActiveDestination = Destinations.Any(x => x.State == DestinationState.Active);
                    if (!existingActiveDestination)
                    {
                        destination.Activate();

                        var routeAdded = new RouteAdded(MessageType, destination.Handler, destination.Endpoint, destination.Sites);
                        yield return new MessageRoutingChanged(routeAdded, new List<RouteRemoved>());
                    }
                }
                else
                {
                    legacyDestination = new Destination(LegacyHandlerType, e.Endpoint, DestinationState.Inactive, MessageKind.Message, new List<string> { e.Site });
                    Destinations.Add(legacyDestination);

                    var existingActiveDestination = Destinations.Any(x => x.State == DestinationState.Active);
                    if (!existingActiveDestination)
                    {
                        legacyDestination.Activate();

                        var routeAdded = new RouteAdded(MessageType, legacyDestination.Handler, legacyDestination.Endpoint, legacyDestination.Sites);
                        yield return new MessageRoutingChanged(routeAdded, new List<RouteRemoved>());
                    }
                }
            }
        }
    }
}