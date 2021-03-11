using System.Collections.Generic;

namespace ExactlyOnce.Routing.ApiContract
{
    public class MessageRoutingInfo
    {
        public string MessageType { get; set; }
        public List<DestinationInfo> Destinations { get; set; }
    }
}