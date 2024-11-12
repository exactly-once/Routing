using NServiceBus;

namespace Contracts
{
    public class PaymentAuthorized : IEvent
    {
        public string OrderId { get; set; }
    }
}