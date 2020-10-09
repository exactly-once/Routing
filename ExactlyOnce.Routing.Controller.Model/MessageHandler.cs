using System;
using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageHandler
    {
        public MessageHandler(string name, string handledMessage, string site)
        {
            Name = name;
            HandledMessage = handledMessage;
            Site = site;
        }

        public string Name { get; }
        public string HandledMessage { get; }
        public string Site { get; }

        sealed class NameHandledMessageEqualityComparer : IEqualityComparer<MessageHandler>
        {
            public bool Equals(MessageHandler x, MessageHandler y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Name == y.Name && x.HandledMessage == y.HandledMessage;
            }

            public int GetHashCode(MessageHandler obj)
            {
                return HashCode.Combine(obj.Name, obj.HandledMessage);
            }
        }

        public static IEqualityComparer<MessageHandler> EqualityComparer { get; } = new NameHandledMessageEqualityComparer();
    }
}