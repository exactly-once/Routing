using System.Collections.Generic;

namespace TestClient
{
    public class EndpointReportRequest
    {
        public string ReportId { get; set; }
        public string EndpointName { get; set; }
        public string InstanceId { get; set; }
        public Dictionary<string, string> MessageHandlers { get; set; }
        public Dictionary<string, MessageKind> RecognizedMessages { get; set; }
    }
}