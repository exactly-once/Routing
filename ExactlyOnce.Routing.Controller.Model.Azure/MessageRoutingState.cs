using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class MessageRoutingState : EventDrivenState<MessageRouting>
    {
        public MessageRoutingState(string messageType)
            : base(new Inbox(), new Outbox(), new MessageRouting(messageType, new List<Destination>()), messageType)
        {
        }

        public MessageRoutingState(MessageRouting messageRouting, Inbox inbox, Outbox outbox)
            : base(inbox, outbox, messageRouting, messageRouting.MessageType)
        {
            MessageRouting = messageRouting;
        }

        public MessageRouting MessageRouting { get; }
    }
}