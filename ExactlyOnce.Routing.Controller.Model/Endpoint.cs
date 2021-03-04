using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class Endpoint
    {
        //Used by deserialization
        [JsonConstructor]
        public Endpoint(string name, Dictionary<string, EndpointInstance> instances, Dictionary<string, MessageKind> recognizedMessages, LegacyAutoSubscribeState autoSubscribe)
        {
            Name = name;
            Instances = instances;
            RecognizedMessages = recognizedMessages;
            AutoSubscribe = autoSubscribe;
        }

        // Used by event loop
        // ReSharper disable once UnusedMember.Global
        public Endpoint()
        {
        }

        public Endpoint(string name)
            : this(name, new Dictionary<string, EndpointInstance>(), new Dictionary<string, MessageKind>(), LegacyAutoSubscribeState.NotSet)
        {
        }

        public string Name { get; }
        public Dictionary<string, EndpointInstance> Instances { get; }
        public Dictionary<string, MessageKind> RecognizedMessages { get; }
        public LegacyAutoSubscribeState AutoSubscribe { get; private set; }

        public IEnumerable<IEvent> OnHello(string instanceId, string site)
        {
            if (!Instances.TryGetValue(instanceId, out var instance))
            {
                instance = new EndpointInstance(instanceId, null, new List<MessageHandlerInstance>(), new Dictionary<string, MessageKind>(), site);
                Instances[instanceId] = instance;

                return Enumerable.Empty<IEvent>();
            }

            var result = GenerateInstanceLocationUpdated(instanceId, instance.Site, site, instance.InputQueue, instance.InputQueue);

            if (instance.Site == null)
            {
                var handlersBefore = DeriveMessageHandlers(site);
                instance.Move(site);
                result = result
                    .Concat(ComputeMessageHandlerChanges(site, handlersBefore, DeriveMessageHandlers(site)));
            }
            else if (instance.Site != site)
            {
                //Instance moved to a different site
                var previousSite = instance.Site;
                var handlersInSourceSite = DeriveMessageHandlers(previousSite);
                var handlersInDestinationSite = DeriveMessageHandlers(site);

                instance.Move(site);

                result = result
                    .Concat(ComputeMessageHandlerChanges(previousSite, handlersInSourceSite,DeriveMessageHandlers(previousSite))
                    .Concat(ComputeMessageHandlerChanges(site, handlersInDestinationSite, DeriveMessageHandlers(site))));
            }

            return result;
        }

        public IEnumerable<IEvent> OnStartup(string instanceId, string inputQueue,
            Dictionary<string, MessageKind> recognizedMessages, List<MessageHandlerInstance> messageHandlers, bool autoSubscribe)
        {
            if (AutoSubscribe == LegacyAutoSubscribeState.NotSet)
            {
                AutoSubscribe =
                    autoSubscribe ? LegacyAutoSubscribeState.Subscribe : LegacyAutoSubscribeState.DoNotSubscribe;
            }

            List<string> affectedMessageTypes;
            if (!Instances.TryGetValue(instanceId, out var instance))
            {
                instance = new EndpointInstance(instanceId, inputQueue, messageHandlers, recognizedMessages, null);
                Instances[instanceId] = instance;
                affectedMessageTypes = instance.RecognizedMessages.Keys.ToList();
                return DeriveMessageKindsChanges(affectedMessageTypes);
            }

            affectedMessageTypes = instance.Update(messageHandlers, recognizedMessages, inputQueue);

            var result = 
                GenerateInstanceLocationUpdated(instanceId, instance.Site, instance.Site, instance.InputQueue, inputQueue)
                    .Concat(DeriveMessageKindsChanges(affectedMessageTypes));

            if (instance.Site != null)
            {
                var previousHandlers = DeriveMessageHandlers(instance.Site);
                result = result.Concat(ComputeMessageHandlerChanges(instance.Site, previousHandlers, DeriveMessageHandlers(instance.Site)));
            }

            return result;
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

        IEnumerable<IEvent> GenerateInstanceLocationUpdated(string instanceId, string previousSite, string currentSite, string previousQueue, string currentQueue)
        {
            if (currentSite == null || currentQueue == null)
            {
                //We don't publish event if site or queue is not yet assigned
                yield break;
            }

            if (previousSite != currentSite || previousQueue != currentQueue)
            {
                yield return new EndpointInstanceLocationUpdated(Name, instanceId, currentSite, currentQueue);
            }
        }

        IEnumerable<IEvent> ComputeMessageHandlerChanges(string site, List<MessageHandlerInstance> oldHandlers, List<MessageHandlerInstance> newHandlers)
        {
            foreach (var addedHandler in newHandlers.Except(oldHandlers, MessageHandlerInstance.NameHandledMessageComparer))
            {
                var messageKind = RecognizedMessages[addedHandler.HandledMessage];
                var autoSubscribe = AutoSubscribe == LegacyAutoSubscribeState.Subscribe && messageKind == MessageKind.Event;

                yield return new MessageHandlerAdded(addedHandler.Name, addedHandler.HandledMessage, 
                    messageKind, Name, site, autoSubscribe);
            }

            foreach (var removedHandler in oldHandlers.Except(newHandlers, MessageHandlerInstance.NameHandledMessageComparer))
            {
                yield return new MessageHandlerRemoved(removedHandler.Name, removedHandler.HandledMessage, Name, site);
            }

            if (AutoSubscribe == LegacyAutoSubscribeState.Subscribe)
            {
                AutoSubscribe = LegacyAutoSubscribeState.Subscribed;
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