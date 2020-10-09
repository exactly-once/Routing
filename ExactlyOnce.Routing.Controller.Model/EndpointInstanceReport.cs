using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class EndpointInstanceReport
    {
        public string UniqueId { get; set; }
        public string EndpointName { get; set; }
        public string InstanceId { get; set; }
        public List<MessageHandlerInstance> MessageHandlers { get; set; }
        public List<MessageType> RecognizedMessages { get; }
    }
}