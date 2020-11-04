using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterInfo
    {
        [JsonConstructor]
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