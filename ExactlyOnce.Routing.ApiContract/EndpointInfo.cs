using System.Collections.Generic;

namespace ExactlyOnce.Routing.ApiContract
{
    public class EndpointInfo
    {
        public string Name { get; set; }
        public Dictionary<string, EndpointInstanceInfo> Instances { get; set; }
        public Dictionary<string, MessageKind> RecognizedMessages { get; set; }
    }

    public class EndpointInstanceInfo
    {
        public string InstanceId { get; set; }
        public string InputQueue { get; set; }
        public string Site { get; set; }
        public List<MessageHandlerInstanceInfo> MessageHandlers { get; set; }
        public Dictionary<string, MessageKind> RecognizedMessages { get; set; }
    }

    public class MessageHandlerInstanceInfo
    {
        public string Name { get; set; }
        public string HandledMessage { get; set; }
    }
}