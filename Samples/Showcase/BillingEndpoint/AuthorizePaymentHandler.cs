using System.Threading.Tasks;
using Contracts;
using NServiceBus;
using NServiceBus.Logging;

namespace BillingEndpoint
{
    public class AuthorizePaymentHandler : IHandleMessages<AuthorizePayment>
    {
        public Task Handle(AuthorizePayment message, IMessageHandlerContext context)
        {
            log.Info($"Payment for order {message.OrderId} USD {message.OrderTotal} authorized.");
            return context.Publish(new PaymentAuthorized
            {
                OrderId = message.OrderId
            });
        }

        static readonly ILog log = LogManager.GetLogger<AuthorizePaymentHandler>();
    }
}