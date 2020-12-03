using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.SelfHostedController
{
    public class SignalRHandler : IMessageHandler
    {
        readonly IHubContext<NotificationHub> hubContext;
        readonly ILogger<SignalRHandler> log;

        public SignalRHandler(IHubContext<NotificationHub> hubContext, ILogger<SignalRHandler> log)
        {
            this.hubContext = hubContext;
            this.log = log;
        }

        public async Task Handle(EventMessage eventMessage, ISender sender)
        {
            var e = eventMessage.Payload as RoutingTableChanged;
            if (e == null)
            {
                //We only care about that specific event for now
                return;
            }

            var arg = new RoutingTableUpdated
            {
                JsonContent = JsonConvert.SerializeObject(e)
            };

            await hubContext.Clients.All.SendCoreAsync("routeTableUpdated", new object[] {arg}).ConfigureAwait(false);
        }

        public class RoutingTableUpdated
        {
            public string JsonContent { get; set; }
        }
    }
}