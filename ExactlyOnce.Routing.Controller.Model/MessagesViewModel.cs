using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class EndpointViewModel
    {
        
    }

    public class EndpointInstanceViewModel
    {

    }

    public class MessagesViewModel
    {
        public MessagesViewModel(List<MessageViewModel> messages)
        {
            Messages = messages;
        }

        public List<MessageViewModel> Messages { get; }
    }

    public class MessageViewModel
    {
        public MessageViewModel(string name, Dictionary<string, MessageKind> kindByEndpoint)
        {
            Name = name;
            KindByEndpoint = kindByEndpoint;
        }

        public string Name { get; }
        public Dictionary<string, MessageKind> KindByEndpoint { get; }
    }
}