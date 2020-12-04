using System.Collections.Generic;

namespace ExactlyOnce.Routing.Client
{
    public class MessageDestinations
    {
        public string MessageType { get; set; }
        public List<Destination> Destinations { get; set; }
    }
}