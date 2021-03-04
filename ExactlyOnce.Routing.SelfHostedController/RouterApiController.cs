using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.Routing.ApiContract;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.Routing.SelfHostedController
{
    [ApiController]
    public class RouterApiController : ControllerBase
    {
        readonly ISender sender;
        readonly OnceExecutorFactory executorFactory;
        readonly ILogger<RouterApiController> logger;

        public RouterApiController(OnceExecutorFactory executorFactory, ISender sender,
            ILogger<RouterApiController> logger)
        {
            this.executorFactory = executorFactory;
            this.logger = logger;
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

        
    }
}