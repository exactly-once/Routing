using System.Collections.Generic;
using Contracts;
using NServiceBus;

namespace OrderSaga
{
    public class OrderSagaData : ContainSagaData
    {
        public string OrderId { get; set; }
        public List<Item> Items { get; set; }
        public bool PaymentAuthorized { get; set; }
        public string DeliveryAddress { get; set; }
    }
}
