using System.Collections.Generic;

namespace ExactlyOnce.Routing.Client
{
    public class EndpointReportRequest
    {
        public string ReportId { get; set; }
        public string EndpointName { get; set; }
        public string InputQueue { get; set; }
        public string InstanceId { get; set; }
        public Dictionary<string, string> MessageHandlers { get; set; }
        public Dictionary<string, MessageKind> RecognizedMessages { get; set; }
        public Dictionary<string, string> LegacyDestinations { get; set; }
        public bool AutoSubscribe { get; set; }
    }
}