using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageTypeRemoved : IEvent
    {
        [JsonConstructor]
        public MessageTypeRemoved(string fullName)
        {
            FullName = fullName;
        }

        public string FullName { get; }
    }
}