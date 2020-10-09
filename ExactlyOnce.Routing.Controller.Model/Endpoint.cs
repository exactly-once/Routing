using System;
using System.Collections.Generic;
using System.Linq;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class Endpoint
    {
        public Endpoint(string name, Dictionary<string, EndpointInstance> instances, Dictionary<string, MessageKind> recognizedMessages)
        {
            Name = name;
            Instances = instances;
            RecognizedMessages = recognizedMessages;
        }

        public string Name { get; }
        public Dictionary<string, EndpointInstance> Instances { get; }
        public Dictionary<string, MessageKind> RecognizedMessages { get; }

        public IEnumerable<IEvent> OnEndpointHello(string instanceId, string site)
        {
            if (!Instances.TryGetValue(instanceId, out var instance))
            {
                instance = new EndpointInstance(instanceId, new List<MessageHandlerInstance>(), new Dictionary<string, MessageKind>(), site);
                Instances[instanceId] = instance;

                //We don't know this endpoint handlers yet
                return Enumerable.Empty<IEvent>();
            }

            if (instance.Site == site)
            {
                return Enumerable.Empty<IEvent>(); //No changes
            }

            if (instance.Site == null)
            {
                var handlersBefore = DeriveMessageHandlers(site);
                instance.Move(site);
                return ComputeMessageHandlerChanges(site, handlersBefore, DeriveMessageHandlers(site));
            }

            //Instance moved to a different site
            var previousSite = instance.Site;
            var handlersInSourceSite = DeriveMessageHandlers(previousSite);
            var handlersInDestinationSite = DeriveMessageHandlers(site);

            instance.Move(site);

            return ComputeMessageHandlerChanges(previousSite, handlersInSourceSite, DeriveMessageHandlers(previousSite))
                .Concat(ComputeMessageHandlerChanges(site, handlersInDestinationSite, DeriveMessageHandlers(site)));
        }

        public IEnumerable<IEvent> OnEndpointStartup(string instanceId, Dictionary<string, MessageKind> recognizedMessages, List<MessageHandlerInstance> messageHandlers)
        {
            List<string> affectedMessageTypes;
            if (!Instances.TryGetValue(instanceId, out var instance))
            {
                instance = new EndpointInstance(instanceId, messageHandlers, recognizedMessages, null);
                Instances[instanceId] = instance;
                affectedMessageTypes = instance.RecognizedMessages.Keys.ToList();
                return DeriveMessageKindsChanges(affectedMessageTypes);
            }

            if (instance.Site == null)
            {
                affectedMessageTypes = instance.Update(messageHandlers, recognizedMessages);
                return DeriveMessageKindsChanges(affectedMessageTypes);
            }

            //Instance already assigned to a site
            var previousHandlers = DeriveMessageHandlers(instance.Site);
            affectedMessageTypes = instance.Update(messageHandlers, recognizedMessages);
            return DeriveMessageKindsChanges(affectedMessageTypes)
                .Concat(ComputeMessageHandlerChanges(instance.Site, previousHandlers, DeriveMessageHandlers(instance.Site)));
        }

        public void ValidateSubscribe(string messageType, string handlerType)
        {
            if (!RecognizedMessages.TryGetValue(messageType, out var kind))
            {
                throw new Exception($"Cannot subscribe {handlerType} at {Name} to {messageType} because this message is not recognized by the endpoint.");
            }
            if (kind == MessageKind.Command)
            {
                throw new Exception($"Cannot subscribe {handlerType} at {Name} to {messageType} because this message considered a command by the endpoint.");
            }
        }

        public void ValidateUnsubscribe(string messageType, string handlerType)
        {
            if (!RecognizedMessages.TryGetValue(messageType, out var kind))
            {
                throw new Exception($"Cannot unsubscribe {handlerType} at {Name} from {messageType} because this message is not recognized by the endpoint.");
            }
            if (kind == MessageKind.Command)
            {
                throw new Exception($"Cannot unsubscribe {handlerType} at {Name} from {messageType} because this message considered a command by the endpoint.");
            }
        }

        public void ValidateAppoint(string messageType, string handlerType)
        {
            if (!RecognizedMessages.TryGetValue(messageType, out var kind))
            {
                throw new Exception($"Cannot appoint {handlerType} at {Name} to process {messageType} because this message is not recognized by the endpoint.");
            }
            if (kind == MessageKind.Command)
            {
                throw new Exception($"Cannot appoint {handlerType} at {Name} to process {messageType} because this message considered an event by the endpoint.");
            }
        }

        public void ValidateDismiss(string messageType, string handlerType)
        {
            if (!RecognizedMessages.TryGetValue(messageType, out var kind))
            {
                throw new Exception($"Cannot dismiss {handlerType} at {Name} from processing {messageType} because this message is not recognized by the endpoint.");
            }
            if (kind == MessageKind.Command)
            {
                throw new Exception($"Cannot dismiss {handlerType} at {Name} from processing {messageType} because this message considered an event by the endpoint.");
            }
        }

        IEnumerable<IEvent> DeriveMessageKindsChanges(IEnumerable<string> affectedMessageTypes)
        {
            foreach (var affectedMessageType in affectedMessageTypes)
            {
                var kindsReported = Instances.Values
                    .SelectMany(x => GetKindOrEmptyEnumerable(x, affectedMessageType))
                    .ToArray();

                if (kindsReported.Length == 0)
                {
                    yield return new MessageTypeRemoved(affectedMessageType);
                }
                else
                {
                    var newKind = DeriveMessageKind(kindsReported);
                    if (!RecognizedMessages.TryGetValue(affectedMessageType, out var currentKind))
                    {
                        yield return new MessageTypeAdded(affectedMessageType, newKind, Name);
                        RecognizedMessages[affectedMessageType] = newKind;
                    }
                    else if (currentKind != newKind)
                    {
                        yield return new MessageKindChanged(affectedMessageType, newKind, Name);
                        RecognizedMessages[affectedMessageType] = newKind;
                    }
                }
            }
        }

        static IEnumerable<MessageKind> GetKindOrEmptyEnumerable(EndpointInstance instance, string messageType)
        {
            if (instance.RecognizedMessages.TryGetValue(messageType, out var kind))
            {
                yield return kind;
            }
        }

        static MessageKind DeriveMessageKind(IReadOnlyList<MessageKind> kindsReported)
        {
            //Report undefined if there are any conflicts
            var firstValue = kindsReported[0];
            return kindsReported.Any(x => x != firstValue)
                ? MessageKind.Undefined
                : firstValue;
        }

        IEnumerable<IEvent> ComputeMessageHandlerChanges(string site, List<MessageHandlerInstance> oldHandlers, List<MessageHandlerInstance> newHandlers)
        {
            foreach (var addedHandler in newHandlers.Except(oldHandlers, MessageHandlerInstance.NameHandledMessageComparer))
            {
                yield return new MessageHandlerAdded(addedHandler.Name, addedHandler.HandledMessage, Name, site);
            }

            foreach (var removedHandler in oldHandlers.Except(newHandlers, MessageHandlerInstance.NameHandledMessageComparer))
            {
                yield return new MessageHandlerRemoved(removedHandler.Name, removedHandler.HandledMessage, Name, site);
            }
        }

        List<MessageHandlerInstance> DeriveMessageHandlers(string site)
        {
            //Return only handlers that are present in all instances of the endpoint
            var uniqueHandlers = Instances.Values
                .Where(x => x.Site == site)
                .SelectMany(x => x.MessageHandlers)
                .Distinct(MessageHandlerInstance.NameHandledMessageComparer);

            var hostedInEachInstance = uniqueHandlers
                .Where(x => Instances.Values
                    .Where(x => x.Site == site)
                    .All(i => i.MessageHandlers.Contains(x, MessageHandlerInstance.NameHandledMessageComparer)))
                .ToList();

            return hostedInEachInstance;
        }
    }
}