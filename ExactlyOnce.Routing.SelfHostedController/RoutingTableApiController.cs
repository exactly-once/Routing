using System;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.Routing.SelfHostedController
{
    [ApiController]
    public class RoutingTableApiController : ControllerBase
    {
        readonly ISender sender;
        readonly OnceExecutorFactory executorFactory;
        readonly IStateStore stateStore;
        readonly ILogger<RoutingTableApiController> logger;

        public RoutingTableApiController(OnceExecutorFactory executorFactory, IStateStore stateStore, ISender sender,
            ILogger<RoutingTableApiController> logger)
        {
            this.executorFactory = executorFactory;
            this.stateStore = stateStore;
            this.logger = logger;
            this.sender = sender;
        }

        [HttpPost]
        [Route("ConfigureEndpointSiteRouting")]
        public async Task<IActionResult> ConfigureEndpointSiteRouting(ConfigureEndpointSiteRoutingRequest request)
        {
            var executor =
                executorFactory.CreateGenericExecutor<RoutingTableState, RoutingTable>(request.RequestId, "Instance");

            var messages = await executor.Once(
                r => r.ConfigureSiteRouting(request.EndpointName, request.Policy),
                () => throw new Exception("Routing table not yet created"));

            foreach (var eventMessage in messages)
            {
                await sender.Publish(eventMessage).ConfigureAwait(false);
            }

            return Ok();
        }

        [HttpGet]
        [Route("GetRoutingTable")]
        public async Task<IActionResult> Get()
        {
            var stateId = DeterministicGuid.MakeId("Instance");
            var (state, etag) = await stateStore.Load<RoutingTableState>(stateId.ToString()).ConfigureAwait(false);
            if (etag == null || state.Data == null)
            {
                return NotFound();
            }

            return Ok(state.Data);
        }

        public class ConfigureEndpointSiteRoutingRequest
        {
            public string RequestId { get; set; }
            public string EndpointName { get; set; }
            public string Policy { get; set; }
        }
    }
}