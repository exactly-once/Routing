using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.DelayedDelivery;
using NServiceBus.Extensibility;
using NServiceBus.Performance.TimeToBeReceived;
using NServiceBus.Transport;
using NServiceBus.Unicast.Queuing;

namespace SampleInfrastructure.TestTransport
{
    class TestTransportDispatcher : IDispatchMessages
    {
        public TestTransportDispatcher(string basePath, int maxMessageSizeKB, string brokerName)
        {
            if (maxMessageSizeKB > int.MaxValue / 1024)
            {
                throw new ArgumentException("The message size cannot be larger than int.MaxValue / 1024.", nameof(maxMessageSizeKB));
            }

            this.basePath = basePath;
            this.maxMessageSizeKB = maxMessageSizeKB;
            this.brokerName = brokerName;
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
        {
            return Task.WhenAll(
                DispatchUnicast(outgoingMessages.UnicastTransportOperations, transaction),
                DispatchMulticast(outgoingMessages.MulticastTransportOperations, transaction));
        }

        async Task DispatchMulticast(IEnumerable<MulticastTransportOperation> transportOperations, TransportTransaction transaction)
        {
            var tasks = new List<Task>();

            foreach (var transportOperation in transportOperations)
            {
                if (!transportOperation.Message.Headers.TryGetValue(Headers.MessageId, out var messageId))
                {
                    messageId = transportOperation.Message.MessageId;
                }

                if (!transportOperation.Message.Headers.TryGetValue(Headers.MessageIntent, out var intent))
                {
                    intent = "<Unknown>";
                }

                var subscribers = await GetSubscribersFor(transportOperation.MessageType)
                    .ConfigureAwait(false);

                foreach (var subscriber in subscribers)
                {
                    tasks.Add(WriteMessage(subscriber, transportOperation, transaction));
                    Console.WriteLine($"Publishing message {messageId} with intent {intent} sent to {subscriber}");
                }
            }

            await Task.WhenAll(tasks)
                .ConfigureAwait(false);
        }

        Task DispatchUnicast(IEnumerable<UnicastTransportOperation> operations, TransportTransaction transaction)
        {
            return Task.WhenAll(operations.Select(operation =>
            {
                PathChecker.ThrowForBadPath(operation.Destination, "message destination");

                if (!operation.Message.Headers.TryGetValue(Headers.MessageId, out var messageId))
                {
                    messageId = operation.Message.MessageId;
                }

                if (!operation.Message.Headers.TryGetValue(Headers.MessageIntent, out var intent))
                {
                    intent = "<Unknown>";
                }
                Console.WriteLine($"Sending message {messageId} with intent {intent} sent to {operation.Destination}");

                return WriteMessage(operation.Destination, operation, transaction);
            }));
        }

        async Task WriteMessage(string destination, IOutgoingTransportOperation transportOperation, TransportTransaction transaction)
        {
            var message = transportOperation.Message;

            if (destination.IndexOf("@", StringComparison.Ordinal) != -1)
            {
                var parts = destination.Split(new[] {'@'}, StringSplitOptions.RemoveEmptyEntries);

                var broker = parts[1];

                if (broker != brokerName)
                {
                    throw new Exception($"Attempt to send a message to broker {broker} through transport configured for {brokerName}.");
                }
            }
            else
            {
                //Default to sending to local broker
                destination = $"{destination}@{brokerName}";
            }

            var nativeMessageId = Guid.NewGuid().ToString();
            var destinationPath = Path.Combine(basePath, destination);

            if (!Directory.Exists(destinationPath))
            {
                throw new QueueNotFoundException(destination, "Destination queue does not exist.", null);
            }

            var bodyDir = Path.Combine(destinationPath, TestTransportMessagePump.BodyDirName);

            Directory.CreateDirectory(bodyDir);

            var bodyPath = Path.Combine(bodyDir, nativeMessageId) + TestTransportMessagePump.BodyFileSuffix;

            await AsyncFile.WriteBytes(bodyPath, message.Body)
                .ConfigureAwait(false);

            DateTime? timeToDeliver = null;

            if (transportOperation.DeliveryConstraints.TryGet(out DoNotDeliverBefore doNotDeliverBefore))
            {
                timeToDeliver = doNotDeliverBefore.At;
            }
            else if (transportOperation.DeliveryConstraints.TryGet(out DelayDeliveryWith delayDeliveryWith))
            {
                timeToDeliver = DateTime.UtcNow + delayDeliveryWith.Delay;
            }

            if (timeToDeliver.HasValue)
            {
                // we need to "ceil" the seconds to guarantee that we delay with at least the requested value
                // since the folder name has only second resolution.
                if (timeToDeliver.Value.Millisecond > 0)
                {
                    timeToDeliver += TimeSpan.FromSeconds(1);
                }

                destinationPath = Path.Combine(destinationPath, TestTransportMessagePump.DelayedDirName, timeToDeliver.Value.ToString("yyyyMMddHHmmss"));

                Directory.CreateDirectory(destinationPath);
            }

            if (transportOperation.DeliveryConstraints.TryGet(out DiscardIfNotReceivedBefore timeToBeReceived) && timeToBeReceived.MaxTime < TimeSpan.MaxValue)
            {
                if (timeToDeliver.HasValue)
                {
                    throw new Exception($"Postponed delivery of messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to postpone messages of type '{message.Headers[Headers.EnclosedMessageTypes]}'.");
                }

                message.Headers[TestTransportHeaders.TimeToBeReceived] = timeToBeReceived.MaxTime.ToString();
            }

            var messagePath = Path.Combine(destinationPath, nativeMessageId) + ".metadata.txt";

            var headerPayload = HeaderSerializer.Serialize(message.Headers);
            var headerSize = Encoding.UTF8.GetByteCount(headerPayload);

            if (headerSize + message.Body.Length > maxMessageSizeKB * 1024)
            {
                throw new Exception($"The total size of the '{message.Headers[Headers.EnclosedMessageTypes]}' message body ({message.Body.Length} bytes) plus headers ({headerSize} bytes) is larger than {maxMessageSizeKB} KB and will not be supported on some production transports. Consider using the NServiceBus DataBus or the claim check pattern to avoid messages with a large payload. Use 'EndpointConfiguration.UseTransport<TestTransport>().NoPayloadSizeRestriction()' to disable this check and proceed with the current message size.");
            }

            if (transportOperation.RequiredDispatchConsistency != DispatchConsistency.Isolated && transaction.TryGet(out ITestTransportTransaction directoryBasedTransaction))
            {
                await directoryBasedTransaction.Enlist(messagePath, headerPayload)
                    .ConfigureAwait(false);
            }
            else
            {
                // atomic avoids the file being locked when the receiver tries to process it
                await AsyncFile.WriteTextAtomic(messagePath, headerPayload)
                    .ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<string>> GetSubscribersFor(Type messageType)
        {
            var subscribers = new HashSet<string>();

            var allEventTypes = GetPotentialEventTypes(messageType);

            foreach (var eventType in allEventTypes)
            {
                var eventDir = Path.Combine(basePath, ".events", eventType.FullName);

                if (!Directory.Exists(eventDir))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(eventDir))
                {
                    var allText = await AsyncFile.ReadText(file)
                        .ConfigureAwait(false);

                    subscribers.Add(allText);
                }
            }

            return subscribers;
        }

        static IEnumerable<Type> GetPotentialEventTypes(Type messageType)
        {
            var allEventTypes = new HashSet<Type>();

            var currentType = messageType;

            while (currentType != null)
            {
                //do not include the marker interfaces
                if (IsCoreMarkerInterface(currentType))
                {
                    break;
                }

                allEventTypes.Add(currentType);

                currentType = currentType.BaseType;
            }

            foreach (var type in messageType.GetInterfaces().Where(i => !IsCoreMarkerInterface(i)))
            {
                allEventTypes.Add(type);
            }

            return allEventTypes;
        }

        static bool IsCoreMarkerInterface(Type type) => type == typeof(IMessage) || type == typeof(IEvent) || type == typeof(ICommand);

        int maxMessageSizeKB;
        string brokerName;
        string basePath;
    }
}