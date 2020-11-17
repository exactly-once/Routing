using System.IO;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.AzureController
{
    public class RouteTableStorageApi
    {
        static readonly JsonSerializer serializer = new JsonSerializer();

        [FunctionName(nameof(Store))]
        public async Task Store(
            [QueueTrigger("routing-table-store")] EventMessage eventMessage,
            [Blob("routing-table/routing-table.json", FileAccess.Write)] Stream routingTableFileStream,
            ILogger log)
        {
            var routingTableChangedEvent = eventMessage.Payload as RoutingTableChanged;
            if (routingTableChangedEvent == null)
            {
                //We only care about that specific event
                return;
            }

            await using (var streamWriter = new StreamWriter(routingTableFileStream))
            {
                serializer.Serialize(streamWriter, routingTableChangedEvent);
                await streamWriter.FlushAsync();
            }
        }
    }
}