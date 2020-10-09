using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class RouterPendingChanges
    {
        public RouterPendingChanges(int sequence, List<string> interfacesAdded, List<string> interfacesRemoved)
        {
            Sequence = sequence;
            InterfacesAdded = interfacesAdded;
            InterfacesRemoved = interfacesRemoved;
        }

        public int Sequence { get; }
        public List<string> InterfacesAdded { get; }
        public List<string> InterfacesRemoved { get; }
    }
}