using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Contracts;
using NServiceBus;
using NServiceBus.Logging;

namespace OrderSaga
{
    public class OrderSaga : Saga<OrderSagaData>,
        IAmStartedByMessages<OrderAccepted>,
        IHandleMessages<PaymentAuthorized>,
        IHandleMessages<ShipmentPrepared>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
        {
            mapper.ConfigureMapping<OrderAccepted>(m => m.OrderId).ToSaga(s => s.OrderId);
            mapper.ConfigureMapping<PaymentAuthorized>(m => m.OrderId).ToSaga(s => s.OrderId);
            mapper.ConfigureMapping<ShipmentPrepared>(m => m.OrderId).ToSaga(s => s.OrderId);
        }

        public Task Handle(OrderAccepted message, IMessageHandlerContext context)
        {
            Data.Items = message.Items;
            Data.DeliveryAddress = message.DeliveryAddress;

            log.Info($"Requesting authorization of payment for order {message.OrderId}.");

            return context.Send(new AuthorizePayment
            {
                OrderId = message.OrderId,
                OrderTotal = CalculateOrderTotal()
            });
        }

        decimal CalculateOrderTotal()
        {
            return Data.Items.Sum(x => x.Quantity * (int) x.Type);
        }

        public Task Handle(PaymentAuthorized message, IMessageHandlerContext context)
        {
            log.Info($"Payment authorized for order {message.OrderId}.");

            Data.PaymentAuthorized = true;
            var options = new SendOptions();
            var destinationSite = DetermineDestinationSite();
            options.SendToSite(destinationSite);

            log.Info($"Requesting shipment preparation for order {message.OrderId} from site {destinationSite}.");

            return context.Send(new PrepareShipment
            {
                OrderId = message.OrderId,
                DeliveryAddress = Data.DeliveryAddress,
                Items = Data.Items
            }, options);
        }

        string DetermineDestinationSite()
        {
            if (Data.DeliveryAddress.Length < 2)
            {
                return "global";
            }
            var countryCode = Data.DeliveryAddress.Substring(0, 2).ToLowerInvariant();
            return countryCode switch
            {
                "pl" => "poland",
                "ch" => "switzerland",
                "fr" => "france",
                _ => "global"
            };
        }

        public Task Handle(ShipmentPrepared message, IMessageHandlerContext context)
        {
            log.Info($"Shipment prepared for order {message.OrderId}. Order processing complete.");

            MarkAsComplete();
            return Task.CompletedTask;
        }

        static readonly ILog log = LogManager.GetLogger<OrderSaga>();
    }
}