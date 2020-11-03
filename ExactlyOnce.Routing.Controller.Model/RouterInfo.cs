using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterInfo
    {
        public RouterInfo(List<string> interfaces)
        {
            Interfaces = interfaces;
        }

        public List<string> Interfaces { get; private set; }

        public void UpdateInterfaces(List<string> interfaces)
        {
            Interfaces = interfaces;
        }
    }
}