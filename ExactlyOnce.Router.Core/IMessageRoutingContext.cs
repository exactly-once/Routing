using System;
using NServiceBus.Transport;

namespace ExactlyOnce.Router.Core
{
    /// <summary>
    /// Provides context for message processing
    /// </summary>
    public interface IMessageRoutingContext
    {

        /// <summary>
        /// Received message.
        /// </summary>
        MessageContext ReceivedMessage { get; }
        /// <summary>
        /// Name of the interface that received the message.
        /// </summary>
        string IncomingInterface { get; }
        /// <summary>
        /// Returns reference to the interface based on its name.
        /// </summary>
        IRawEndpoint GetOutgoingInterface(string name);

        /// <summary>
        /// Returns a surrogate dynamically generated that can be used for publishing.
        /// </summary>
        Type GetTypeSurrogate(string typeName);
    }
}