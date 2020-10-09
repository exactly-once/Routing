using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterInstanceReport
    {
        public string UniqueId { get; set; }
        public string RouterName { get; set; }
        public string InstanceId { get; set; }
        public List<string> SiteInterfaces { get; set; }
    }
}