using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterInterfacesChanged : IEvent
    {
        public RouterInterfacesChanged(string router, List<string> interfaces)
        {
            Router = router;
            Interfaces = interfaces;
        }

        public string Router { get; }
        public List<string> Interfaces { get; }
    }
}