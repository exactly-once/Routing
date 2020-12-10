using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.DeliveryConstraints;
using NServiceBus.Logging;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace ExactlyOnce.Routing.NServiceBus
{
    class RoutingToDispatchConnector : StageConnector<IRoutingContext, IDispatchContext>
    {
        public override Task Invoke(IRoutingContext context, Func<IDispatchContext, Task> stage)
        {
            var immediateDispatch = context.Extensions.TryGet("NServiceBus.RoutingToDispatchConnector+State", out object _);

            var dispatchConsistency = immediateDispatch ? DispatchConsistency.Isolated : DispatchConsistency.Default;

            var operations = new TransportOperation[context.RoutingStrategies.Count];
            var index = 0;
            foreach (var strategy in context.RoutingStrategies)
            {
                //If the strategy requires header modification, copy the headers before modification
                var operationHeaders = new Dictionary<string, string>(context.Message.Headers);
                var addressLabel = strategy.Apply(operationHeaders);
                var message = new OutgoingMessage(context.Message.MessageId, operationHeaders, context.Message.Body);
                operations[index] = new TransportOperation(message, addressLabel, dispatchConsistency, context.Extensions.GetDeliveryConstraints());
                index++;
            }

            if (isDebugEnabled)
            {
                LogOutgoingOperations(operations);
            }

            if (!immediateDispatch && context.Extensions.TryGet(out PendingTransportOperations pendingOperations))
            {
                pendingOperations.AddRange(operations);
                return Task.CompletedTask;
            }

            return stage(this.CreateDispatchContext(operations, context));
        }

        static void LogOutgoingOperations(TransportOperation[] operations)
        {
            var sb = new StringBuilder();

            foreach (var operation in operations)
            {
                if (operation.AddressTag is UnicastAddressTag unicastAddressTag)
                {
                    sb.AppendLine($"Destination: {unicastAddressTag.Destination}");
                }

                sb.AppendLine("Message headers:");

                foreach (var kvp in operation.Message.Headers)
                {
                    sb.AppendLine($"{kvp.Key} : {kvp.Value}");
                }
            }

            log.Debug(sb.ToString());
        }

        static readonly ILog log = LogManager.GetLogger<RoutingToDispatchConnector>();
        static readonly bool isDebugEnabled = log.IsDebugEnabled;
    }
}