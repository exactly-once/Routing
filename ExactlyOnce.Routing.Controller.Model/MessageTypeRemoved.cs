namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageTypeRemoved : IEvent
    {
        public MessageTypeRemoved(string fullName)
        {
            FullName = fullName;
        }

        public string FullName { get; }
    }
}