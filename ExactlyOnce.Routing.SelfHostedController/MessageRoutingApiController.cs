using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.Routing.ApiCommon;
using ExactlyOnce.Routing.ApiContract;
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
        readonly IStateStore stateStore;
        readonly ILogger<MessageRoutingApiController> logger;

        public MessageRoutingApiController(OnceExecutorFactory executorFactory, IStateStore stateStore, ISender sender,
            ILogger<MessageRoutingApiController> logger)
        {
            this.executorFactory = executorFactory;
            this.stateStore = stateStore;
            this.logger = logger;
            this.sender = sender;
        }

        [HttpGet]
        [Route("ListMessageTypes/{keyword}")]
        public async Task<IActionResult> ListMessageTypes(string keyword)
        {
            var result = await stateStore.List(typeof(MessageRoutingState), keyword, CancellationToken.None);

            var response = new ListResponse
            {
                Items = result.Select(x => new ListItem
                {
                    Id = x.Id,
                    Name = x.Name
                }).ToList()
            };

            return Ok(response);
        }

        [HttpGet]
        [Route("MessageType/{idOrName}")]
        public async Task<IActionResult> GetMessageType(string idOrName)
        {
            if (!Guid.TryParse(idOrName, out var id))
            {
                id = DeterministicGuid.MakeId(idOrName);
            }
            var (state, etag) = await stateStore.Load<MessageRoutingState>(id.ToString(), CancellationToken.None);
            if (etag == null || state.Data == null)
            {
                return NotFound();
            }

            var destinations = state.Data.Destinations.Select(x => new DestinationInfo
            {
                HandlerType = x.Handler,
                EndpointName = x.Endpoint,
                Active = x.State == DestinationState.Active,
                MessageKind = x.MessageKind.MapMessageKind(),
                Sites = x.Sites
            }).ToList();
            var response = new MessageRoutingInfo
            {
                MessageType = state.Data.MessageType,
                Destinations = destinations
            };

            return Ok(response);
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
    }
}