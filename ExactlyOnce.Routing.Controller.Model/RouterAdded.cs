using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterAdded : IEvent
    {
        [JsonConstructor]
        public RouterAdded(string router, List<string> interfaces)
        {
            Router = router;
            Interfaces = interfaces;
        }

        public string Router { get; }
        public List<string> Interfaces { get; }
    }
}