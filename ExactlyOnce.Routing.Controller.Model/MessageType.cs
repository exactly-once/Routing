using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageType
    {
        public MessageType(string fullName, MessageKind kind)
        {
            FullName = fullName;
            Kind = kind;
        }

        public string FullName { get; }
        public MessageKind Kind { get; }

        sealed class FullNameKindEqualityComparer : IEqualityComparer<MessageType>
        {
            public bool Equals(MessageType x, MessageType y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.FullName == y.FullName && x.Kind == y.Kind;
            }

            public int GetHashCode(MessageType obj)
            {
                unchecked
                {
                    return (obj.FullName.GetHashCode() * 397) ^ (int) obj.Kind;
                }
            }
        }
        public static IEqualityComparer<MessageType> EqualityComparer { get; } = new FullNameKindEqualityComparer();
    }
}