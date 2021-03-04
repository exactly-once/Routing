using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                    kvp => MapMessageKind(kvp.Value));

            var messages = await executor.Once(
                e => e.OnStartup(request.InstanceId, request.InputQueue, messageKinds, messageHandlers, request.AutoSubscribe),
                () => new Endpoint(request.EndpointName));

            foreach (var eventMessage in messages)
            {
                await sender.Publish(eventMessage).ConfigureAwait(false);
            }

            return Ok();
        }

        static MessageKind MapMessageKind(ApiContract.MessageKind value)
        {
            return value switch
            {
                ApiContract.MessageKind.Command => MessageKind.Command,
                ApiContract.MessageKind.Event => MessageKind.Event,
                ApiContract.MessageKind.Message => MessageKind.Message,
                ApiContract.MessageKind.Undefined => MessageKind.Undefined,
                _ => throw new Exception($"Unrecognized message kind: {value}")
            };
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
        [Route("ListEndpoints")]
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
    }
}