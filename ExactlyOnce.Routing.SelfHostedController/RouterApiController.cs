using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.Routing.ApiContract;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RouterInfo = ExactlyOnce.Routing.ApiContract.RouterInfo;
using RouterInstanceInfo = ExactlyOnce.Routing.ApiContract.RouterInstanceInfo;

namespace ExactlyOnce.Routing.SelfHostedController
{
    [ApiController]
    public class RouterApiController : ControllerBase
    {
        readonly ISender sender;
        readonly OnceExecutorFactory executorFactory;
        readonly IStateStore stateStore;
        readonly ILogger<RouterApiController> logger;

        public RouterApiController(OnceExecutorFactory executorFactory, ISender sender,
            IStateStore stateStore,
            ILogger<RouterApiController> logger)
        {
            this.executorFactory = executorFactory;
            this.logger = logger;
            this.stateStore = stateStore;
            this.sender = sender;
        }

        
        [HttpPost]
        [Route("ProcessRouterReport")]
        public async Task<IActionResult> ProcessRouterReport(RouterReportRequest request)
        {
            var executor =
                executorFactory.CreateGenericExecutor<RouterState, Controller.Model.Router>(request.ReportId, request.RouterName);

            var messages = await executor.Once(
                r => r.OnStartup(request.InstanceId, request.SiteInterfaces),
                () => new Controller.Model.Router(request.RouterName));

            foreach (var eventMessage in messages)
            {
                await sender.Publish(eventMessage).ConfigureAwait(false);
            }

            return Ok();
        }

        [HttpGet]
        [Route("ListRouters/{keyword}")]
        public async Task<IActionResult> ListRouters(string keyword)
        {
            var result = await stateStore.List(typeof(RouterState), keyword, CancellationToken.None);

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
        [Route("Router/{id}")]
        public async Task<IActionResult> GetRouter(string idOrName)
        {
            if (!Guid.TryParse(idOrName, out var id))
            {
                id = DeterministicGuid.MakeId(idOrName);
            }
            var (state, etag) = await stateStore.Load<RouterState>(id.ToString(), CancellationToken.None);
            if (etag == null || state.Data == null)
            {
                return NotFound();
            }

            var response = new RouterInfo
            {
                Name = state.Data.Name,
                InterfacesToSites = state.Data.InterfacesToSites,
                Instances = state.Data.Instances.ToDictionary(kvp => kvp.Key, kvp => MapInstance(kvp.Value))
            };

            return Ok(response);
        }

        static RouterInstanceInfo MapInstance(RouterInstance value)
        {
            return new RouterInstanceInfo
            {
                InstanceId = value.InstanceId,
                InterfacesToSites = value.InterfacesToSites
            };
        }
    }
}