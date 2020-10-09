using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class EndpointPendingChanges
    {
        public EndpointPendingChanges(int sequence, List<MessageType> messagesAdded, List<MessageType> messagesRemoved,
            List<MessageHandler> messageHandlersAdded, List<MessageHandler> messageHandlersRemoved)
        {
            MessagesAdded = messagesAdded;
            MessagesRemoved = messagesRemoved;
            MessageHandlersAdded = messageHandlersAdded;
            MessageHandlersRemoved = messageHandlersRemoved;
            Sequence = sequence;
        }
        public int Sequence { get; }
        public List<MessageType> MessagesAdded { get; }
        public List<MessageType> MessagesRemoved { get; }
        public List<MessageHandler> MessageHandlersAdded { get; }
        public List<MessageHandler> MessageHandlersRemoved { get; }
    }
}