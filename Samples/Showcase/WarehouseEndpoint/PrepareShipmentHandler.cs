using System;
using System.Threading.Tasks;
using Contracts;
using NServiceBus;
using NServiceBus.Logging;

namespace WarehouseEndpoint
{
    public class PrepareShipmentHandler : IHandleMessages<PrepareShipment>
    {
        public Task Handle(PrepareShipment message, IMessageHandlerContext context)
        {
            log.Info($"Shipment for order {message.OrderId} prepared.");
            return context.Publish(new ShipmentPrepared
            {
                OrderId = message.OrderId
            });
        }

        static readonly ILog log = LogManager.GetLogger<PrepareShipmentHandler>();
    }
}