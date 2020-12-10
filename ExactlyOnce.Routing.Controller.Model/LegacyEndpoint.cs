using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class LegacyEndpoint
    {
        [JsonConstructor]
        public LegacyEndpoint(string name, List<string> inputQueues, List<string> handledMessages)
        {
            Name = name;
            InputQueues = inputQueues;
            HandledMessages = handledMessages;
        }

        // Used by event loop
        // ReSharper disable once UnusedMember.Global
        public LegacyEndpoint()
        {
        }

        public LegacyEndpoint(string name)
            : this(name, new List<string>(), new List<string>())
        {
        }

        public string Name { get; }
        public List<string> InputQueues { get; }
        public List<string> HandledMessages { get; }

        public IEnumerable<IEvent> RegisterDestination(string messageType, string inputQueue, string site)
        {
            if (!InputQueues.Contains(inputQueue))
            {
                InputQueues.Add(inputQueue);
                yield return new EndpointInstanceLocationUpdated(Name, null, site, inputQueue);
            }

            if (!HandledMessages.Contains(messageType))
            {
                HandledMessages.Add(messageType);
                yield return new LegacyDestinationAdded(messageType, MessageKind.Message, Name, site);
            }
        }
    }
}