using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.Routing.ApiCommon;
using ExactlyOnce.Routing.ApiContract;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MessageKind = ExactlyOnce.Routing.Controller.Model.MessageKind;

namespace ExactlyOnce.Routing.SelfHostedController
{
    [ApiController]
    public class EndpointApiController : ControllerBase
    {
        readonly ISender sender;
        readonly OnceExecutorFactory executorFactory;
        readonly IStateStore stateStore;
        readonly ILogger<EndpointApiController> logger;

        public EndpointApiController(OnceExecutorFactory executorFactory, ISender sender,
            IStateStore stateStore,
            ILogger<EndpointApiController> logger)
        {
            this.executorFactory = executorFactory;
            this.logger = logger;
            this.stateStore = stateStore;
            this.sender = sender;
        }

        [HttpPost]
        [Route("ProcessEndpointReport")]
        public async Task<IActionResult> ProcessEndpointReport(EndpointReportRequest request)
        {
            var executor =
                executorFactory.CreateGenericExecutor<EndpointState, Endpoint>(request.ReportId, request.EndpointName);

            var messageHandlers = request.MessageHandlers != null
                ? request.MessageHandlers.Select(kvp => new MessageHandlerInstance(kvp.Key, kvp.Value)).ToList()
                : new List<MessageHandlerInstance>();

            var messageKinds = request.RecognizedMessages == null
                ? new Dictionary<string, MessageKind>()
                : request.RecognizedMessages.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.MapMessageKind());

            var messages = await executor.Once(
                e => e.OnStartup(request.InstanceId, request.InputQueue, messageKinds, messageHandlers, request.AutoSubscribe),
                () => new Endpoint(request.EndpointName));

            foreach (var eventMessage in messages)
            {
                await sender.Publish(eventMessage).ConfigureAwait(false);
            }

            return Ok();
        }

        
        [HttpPost]
        [Route("ProcessEndpointHello")]
        public async Task<IActionResult> ProcessEndpointHello(EndpointHelloRequest request)
        {
            var executor =
                executorFactory.CreateGenericExecutor<EndpointState, Endpoint>(request.ReportId, request.EndpointName);

            var messages = await executor.Once(
                e => e.OnHello(request.InstanceId, request.Site),
                () => new Endpoint(request.EndpointName));

            foreach (var eventMessage in messages)
            {
                await sender.Publish(eventMessage).ConfigureAwait(false);
            }

            return Ok();
        }

        [HttpPost]
        [Route("RegisterLegacyDestination")]
        public async Task<IActionResult> RegisterLegacyDestination(LegacyDestinationRequest request)
        {
            var executor =
                executorFactory.CreateGenericExecutor<LegacyEndpointState, LegacyEndpoint>(request.RequestId, request.DestinationEndpointName);

            var messages = await executor.Once(
                e => e.RegisterDestination(request.MessageType, request.DestinationQueue, request.Site),
                () => new LegacyEndpoint(request.DestinationEndpointName));

            foreach (var eventMessage in messages)
            {
                await sender.Publish(eventMessage).ConfigureAwait(false);
            }

            return Ok();
        }

        [HttpGet]
        [Route("ListEndpoints/{keyword}")]
        public async Task<IActionResult> ListEndpoints(string keyword)
        {
            var result = await stateStore.List(typeof(EndpointState), keyword, CancellationToken.None);

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
        [Route("Endpoint/{idOrName}")]
        public async Task<IActionResult> GetEndpoint(string idOrName)
        {
            if (!Guid.TryParse(idOrName, out var id))
            {
                id = DeterministicGuid.MakeId(idOrName);
            }
            var(state, etag) = await stateStore.Load<EndpointState>(id.ToString(), CancellationToken.None);
            if (etag == null || state.Data == null)
            {
                return NotFound();
            }

            var response = new EndpointInfo
            {
                Name = state.Data.Name,
                RecognizedMessages = state.Data.RecognizedMessages.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.MapMessageKind()),
                Instances = state.Data.Instances.ToDictionary(kvp => kvp.Key, kvp => MapInstance(kvp.Value))
            };

            return Ok(response);
        }

        static EndpointInstanceInfo MapInstance(EndpointInstance value)
        {
            return new EndpointInstanceInfo
            {
                InputQueue = value.InputQueue,
                InstanceId = value.InstanceId,
                Site = value.Site,
                RecognizedMessages = value.RecognizedMessages.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.MapMessageKind()),
                MessageHandlers = value.MessageHandlers.Select(MapHandler).ToList()
            };
        }

        static MessageHandlerInstanceInfo MapHandler(MessageHandlerInstance value)
        {
            return new MessageHandlerInstanceInfo
            {
                Name = value.Name, 
                HandledMessage = value.HandledMessage
            };
        }
    }
}