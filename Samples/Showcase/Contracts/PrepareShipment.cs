using System.Collections.Generic;
using NServiceBus;

namespace Contracts
{
    public class PrepareShipment : ICommand
    {
        public string OrderId { get; set; }
        public string DeliveryAddress { get; set; }
        public List<Item> Items { get; set; }
    }
}