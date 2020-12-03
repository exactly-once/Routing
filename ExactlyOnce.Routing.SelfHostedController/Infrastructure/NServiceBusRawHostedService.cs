using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.Router.Core;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Routing;
using NServiceBus.Transport;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace ExactlyOnce.Routing.SelfHostedController
{
    public class NServiceBusRawHostedService<T> : IHostedService
        where T : TransportDefinition, new()
    {
        readonly Func<Task> onStarting;
        readonly EventLoopHandler eventLoopHandler;
        readonly Dictionary<string, IMessageHandler> messageHandlers;
        readonly RawEndpointConfiguration config;
        static readonly JsonSerializer serializer = new JsonSerializer();

        IReceivingRawEndpoint receivingEndpoint;
        IRawEndpoint sender;

        public NServiceBusRawHostedService(
            string queueName, 
            Action<TransportExtensions<T>> transportCustomization,
            Func<Task> onStarting,
            EventLoopHandler eventLoopHandler,
            IMessageHandler[] messageHandlers)
        {
            this.onStarting = onStarting;
            this.eventLoopHandler = eventLoopHandler;
            this.messageHandlers = messageHandlers.ToDictionary(x => x.GetType().FullName, x => x);
            config = RawEndpointConfiguration.Create(queueName, HandleMessage, "poison");
            var extensions = config.UseTransport<T>();
            transportCustomization(extensions);
        }

        Task HandleMessage(MessageContext message, IDispatchMessages dispatcher)
        {
            var eventMessage = serializer.Deserialize<EventMessage>(new JsonTextReader(new StreamReader(new MemoryStream(message.Body))));

            return messageHandlers.TryGetValue(eventMessage.DestinationType, out var statelessHandler) 
                ? statelessHandler.Handle(eventMessage, Sender) 
                : eventLoopHandler.Handle(eventMessage, Sender);
        }

        public ISender Sender
        {
            get
            {
                if (sender == null)
                {
                    throw new Exception("Endpoint has not been initialized yet.");
                }
                return new SenderImpl(sender);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await onStarting().ConfigureAwait(false);
            var endpoint = await RawEndpoint.Create(config).ConfigureAwait(false);
            sender = endpoint;
            receivingEndpoint = await endpoint.Start().ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return receivingEndpoint.Stop();
        }

        class SenderImpl : ISender
        {
            readonly IRawEndpoint rawEndpoint;

            public SenderImpl(IRawEndpoint rawEndpoint)
            {
                this.rawEndpoint = rawEndpoint;
            }

            public async Task Publish(EventMessage eventMessage)
            {
                using (var stream = Memory.Manager.GetStream())
                {
                    using (var writer = new StreamWriter(stream, leaveOpen:true))
                    {
                        serializer.Serialize(writer, eventMessage);
                        await writer.FlushAsync();
                    }

                    stream.Seek(0, SeekOrigin.Begin);

                    var body = stream.ToArray();

                    var message = new OutgoingMessage(eventMessage.UniqueId, new Dictionary<string, string>(), body);
                    var op = new TransportOperation(message, new UnicastAddressTag(rawEndpoint.TransportAddress));
                    await rawEndpoint.Dispatch(new TransportOperations(op), new TransportTransaction(),
                        new ContextBag()).ConfigureAwait(false);
                }
            }
        }
    }
}