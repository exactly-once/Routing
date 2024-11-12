using System;
using NServiceBus;

namespace Contracts
{
    public class ShipmentPrepared : IEvent
    {
        public string OrderId { get; set; }
    }
}
