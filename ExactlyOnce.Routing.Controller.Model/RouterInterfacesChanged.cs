using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterInterfacesChanged : IEvent
    {
        [JsonConstructor]
        public RouterInterfacesChanged(string router, List<string> interfaces)
        {
            Router = router;
            Interfaces = interfaces;
        }

        public string Router { get; }
        public List<string> Interfaces { get; }
    }
}