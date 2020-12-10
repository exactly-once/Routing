using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

namespace ExactlyOnce.Routing.AcceptanceTesting.TestPersistence
{
    class TestSubscriptionStorage : ISubscriptionStorage
    {
        public TestSubscriptionStorage(string basePath)
        {
            this.basePath = basePath;
        }

        string GetSubscriptionEntryPath(string eventDir, string endpointName) => Path.Combine(eventDir, endpointName + ".subscription");

        string GetEventDirectory(MessageType eventType) => Path.Combine(basePath, eventType.TypeName);

        readonly string basePath;
        public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var eventDir = GetEventDirectory(messageType);

            // that way we can detect that there is indeed a publisher for the event. That said it also means that we will have do "retries" here due to race condition.
            Directory.CreateDirectory(eventDir);

            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir, subscriber.Endpoint);

            var attempts = 0;

            // since we have a design that can run into concurrency exceptions we perform a few retries
            while (true)
            {
                try
                {
                    await AsyncFile.WriteText(subscriptionEntryPath, subscriber.TransportAddress).ConfigureAwait(false);

                    return;
                }
                catch (IOException)
                {
                    attempts++;

                    if (attempts > 10)
                    {
                        throw;
                    }

                    //allow the other task to complete
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }

        public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var eventDir = GetEventDirectory(messageType);
            var subscriptionEntryPath = GetSubscriptionEntryPath(eventDir, subscriber.Endpoint);

            var attempts = 0;

            // since we have a design that can run into concurrency exceptions we perform a few retries
            while (true)
            {
                try
                {
                    if (!File.Exists(subscriptionEntryPath))
                    {
                        return;
                    }

                    File.Delete(subscriptionEntryPath);

                    return;
                }
                catch (IOException)
                {
                    attempts++;

                    if (attempts > 10)
                    {
                        throw;
                    }

                    //allow the other task to complete
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }

        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> allEventTypes, ContextBag context)
        {
            var subscribers = new Dictionary<string, string>();

            foreach (var eventType in allEventTypes)
            {
                var eventDir = Path.Combine(basePath, eventType.TypeName);

                if (!Directory.Exists(eventDir))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(eventDir))
                {
                    var allText = await AsyncFile.ReadText(file)
                        .ConfigureAwait(false);

                    subscribers[Path.GetFileNameWithoutExtension(file)] = allText;
                }
            }

            return subscribers.Select(kvp => new Subscriber(kvp.Value, kvp.Key));
        }
    }
}