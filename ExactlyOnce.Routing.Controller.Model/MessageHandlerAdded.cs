namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageHandlerAdded : IEvent
    {
        public MessageHandlerAdded(string handlerType, string handledMessageType, MessageKind messageKind,
            string endpoint, string site)
        {
            HandlerType = handlerType;
            HandledMessageType = handledMessageType;
            Endpoint = endpoint;
            Site = site;
            MessageKind = messageKind;
        }

        public string HandlerType { get; }
        public string HandledMessageType { get; }
        public MessageKind MessageKind { get; }
        public string Site { get; }
        public string Endpoint { get; }
    }
}