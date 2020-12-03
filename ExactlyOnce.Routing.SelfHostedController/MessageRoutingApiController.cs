using System;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.Routing.SelfHostedController
{
    [ApiController]
    public class MessageRoutingApiController : ControllerBase
    {
        readonly ISender sender;
        readonly OnceExecutorFactory executorFactory;
        readonly ILogger<MessageRoutingApiController> logger;

        public MessageRoutingApiController(OnceExecutorFactory executorFactory, ISender sender,
            ILogger<MessageRoutingApiController> logger)
        {
            this.executorFactory = executorFactory;
            this.logger = logger;
            this.sender = sender;
        }

        [HttpPost]
        [Route("Subscribe")]
        public async Task<IActionResult> Subscribe(SubscribeRequest request)
        {
            var executor =
                executorFactory.CreateGenericExecutor<MessageRoutingState, MessageRouting>(request.RequestId, request.MessageType);

            var messages = await executor.Once(
                x => x.Subscribe(request.HandlerType, request.Endpoint, request.ReplacedHandlerType),
                () => throw new Exception($"Message type not recognized {request.MessageType}"));

            foreach (var eventMessage in messages)
            {
                await sender.Publish(eventMessage).ConfigureAwait(false);
            }

            return Ok();
        }

        [HttpPost]
        [Route("Unsubscribe")]
        public async Task<IActionResult> Unsubscribe(UnsubscribeRequest request)
        {
            var executor =
                executorFactory.CreateGenericExecutor<MessageRoutingState, MessageRouting>(request.RequestId, request.MessageType);

            var messages = await executor.Once(
                x => x.Unsubscribe(request.HandlerType, request.Endpoint),
                () => throw new Exception($"Message type not recognized {request.MessageType}"));

            foreach (var eventMessage in messages)
            {
                await sender.Publish(eventMessage).ConfigureAwait(false);
            }

            return Ok();
        }

        [HttpPost]
        [Route("Appoint")]
        public async Task<IActionResult> Appoint(AppointRequest request)
        {
            var executor =
                executorFactory.CreateGenericExecutor<MessageRoutingState, MessageRouting>(request.RequestId, request.MessageType);

            var messages = await executor.Once(
                x => x.Appoint(request.HandlerType, request.Endpoint),
                () => throw new Exception($"Message type not recognized {request.MessageType}"));

            foreach (var eventMessage in messages)
            {
                await sender.Publish(eventMessage).ConfigureAwait(false);
            }

            return Ok();
        }

        [HttpPost]
        [Route("Dismiss")]
        public async Task<IActionResult> Dismiss(DismissRequest request)
        {
            var executor =
                executorFactory.CreateGenericExecutor<MessageRoutingState, MessageRouting>(request.RequestId, request.MessageType);

            var messages = await executor.Once(
                x => x.Dismiss(request.HandlerType, request.Endpoint),
                () => throw new Exception($"Message type not recognized {request.MessageType}"));

            foreach (var eventMessage in messages)
            {
                await sender.Publish(eventMessage).ConfigureAwait(false);
            }

            return Ok();
        }

        public class SubscribeRequest
        {
            public string RequestId { get; set; }
            public string MessageType { get; set; }
            public string Endpoint { get; set; }
            public string HandlerType { get; set; }
            public string ReplacedHandlerType { get; set; }
        }

        public class UnsubscribeRequest
        {
            public string RequestId { get; set; }
            public string MessageType { get; set; }
            public string Endpoint { get; set; }
            public string HandlerType { get; set; }
        }

        public class AppointRequest
        {
            public string RequestId { get; set; }
            public string MessageType { get; set; }
            public string Endpoint { get; set; }
            public string HandlerType { get; set; }
        }

        public class DismissRequest
        {
            public string RequestId { get; set; }
            public string MessageType { get; set; }
            public string Endpoint { get; set; }
            public string HandlerType { get; set; }
        }
    }
}