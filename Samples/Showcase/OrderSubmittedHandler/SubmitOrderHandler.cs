using System;
using System.Threading.Tasks;
using Contracts;
using NServiceBus;
using NServiceBus.Logging;

namespace OrderSubmittedHandler
{
    public class SubmitOrderHandler : IHandleMessages<SubmitOrder>
    {
        public Task Handle(SubmitOrder message, IMessageHandlerContext context)
        {
            log.Info($"Order {message.OrderId} accepted.");

            return context.Publish(new OrderAccepted
            {
                OrderId = message.OrderId,
                DeliveryAddress = message.DeliveryAddress,
                Items = message.Items
            });
        }

        static readonly ILog log = LogManager.GetLogger<SubmitOrderHandler>();
    }
}
