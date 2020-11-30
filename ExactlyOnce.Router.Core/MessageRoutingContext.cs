using System;
using System.Collections.Generic;
using NServiceBus.Transport;

namespace ExactlyOnce.Router.Core
{
    class MessageRoutingContext : IMessageRoutingContext
    {
        readonly Dictionary<string, IRawEndpoint> endpoints;
        readonly RuntimeTypeGenerator typeGenerator;

        public MessageRoutingContext(MessageContext receivedMessage, string incomingInterface, Dictionary<string, IRawEndpoint> endpoints, RuntimeTypeGenerator typeGenerator)
        {
            this.endpoints = endpoints;
            this.typeGenerator = typeGenerator;
            ReceivedMessage = receivedMessage;
            IncomingInterface = incomingInterface;
        }

        public MessageContext ReceivedMessage { get; }
        public string IncomingInterface { get; }
        public IRawEndpoint GetOutgoingInterface(string name)
        {
            if (!endpoints.TryGetValue(name, out var endpoint))
            {
                throw new Exception("Interface not configured: " + name);
            }

            return endpoint;
        }

        public Type GetTypeSurrogate(string typeName)
        {
            return typeGenerator.GetType(typeName);
        }
    }
}