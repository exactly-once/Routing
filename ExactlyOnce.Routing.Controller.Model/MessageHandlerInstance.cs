using System;
using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageHandlerInstance
    {
        public MessageHandlerInstance(string name, string handledMessage)
        {
            Name = name;
            HandledMessage = handledMessage;
        }

        public string Name { get; }
        public string HandledMessage { get; }

        sealed class NameHandledMessageEqualityComparer : IEqualityComparer<MessageHandlerInstance>
        {
            public bool Equals(MessageHandlerInstance x, MessageHandlerInstance y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Name == y.Name && x.HandledMessage == y.HandledMessage;
            }

            public int GetHashCode(MessageHandlerInstance obj)
            {
                return HashCode.Combine(obj.Name, obj.HandledMessage);
            }
        }

        public static IEqualityComparer<MessageHandlerInstance> NameHandledMessageComparer { get; } = new NameHandledMessageEqualityComparer();
    }
}