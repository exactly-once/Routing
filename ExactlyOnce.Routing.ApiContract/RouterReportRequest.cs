using System.Collections.Generic;

namespace ExactlyOnce.Routing.ApiContract
{
    public class RouterReportRequest
    {
        public string ReportId { get; set; }
        public string RouterName { get; set; }
        public string InstanceId { get; set; }
        public Dictionary<string, string> SiteInterfaces { get; set; }
    }
}