using NServiceBus;

namespace Contracts
{
    public class AuthorizePayment : ICommand
    {
        public string OrderId { get; set; }
        public decimal OrderTotal { get; set; }
    }
}