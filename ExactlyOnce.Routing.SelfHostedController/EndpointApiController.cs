using System.Collections.Generic;
using System.Linq;
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
        readonly ILogger<EndpointApiController> logger;

        public EndpointApiController(OnceExecutorFactory executorFactory, ISender sender,
            ILogger<EndpointApiController> logger)
        {
            this.executorFactory = executorFactory;
            this.logger = logger;
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

            var messageKinds = request.RecognizedMessages ?? new Dictionary<string, MessageKind>(); //TODO: Map enums

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
    }
}