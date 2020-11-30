using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.AzureController
{
    public class RouteTableStorageApi
    {
        static readonly JsonSerializer serializer = new JsonSerializer();

        [FunctionName(nameof(Store))]
        public async Task Store(
            [QueueTrigger("routing-table-store")] EventMessage eventMessage,
            [Blob("routing-table/routing-table.json", FileAccess.Write)] CloudBlockBlob routingTableBlob,
            ILogger log)
        {
            var routingTableChangedEvent = eventMessage.Payload as RoutingTableChanged;
            if (routingTableChangedEvent == null)
            {
                //We only care about that specific event
                return;
            }

            try
            {
                //Throws 404 if blob does not exist
                await routingTableBlob.FetchAttributesAsync().ConfigureAwait(false);
                await Update(routingTableChangedEvent, routingTableBlob);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int) HttpStatusCode.NotFound)
                {
                    await Create(routingTableChangedEvent, routingTableBlob);
                }
                else
                {
                    throw new Exception("Error while trying to store routing table snapshot", ex);
                }
            }

            
        }

        static async Task Create(RoutingTableChanged e, CloudBlockBlob blob)
        {
            blob.Metadata["Version"] = e.Version.ToString();

            await Upload(e, blob, AccessCondition.GenerateIfNotExistsCondition())
                .ConfigureAwait(false);
        }

        static async Task Update(RoutingTableChanged routingTableChangedEvent, CloudBlockBlob blob)
        {
            var version = int.Parse(blob.Metadata["Version"]);
            if (routingTableChangedEvent.Version <= version)
            {
                return;
            }

            await Upload(routingTableChangedEvent, blob, AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag))
                .ConfigureAwait(false);
        }

        static async Task Upload(RoutingTableChanged e, CloudBlockBlob blob, AccessCondition condition)
        {
            using (var stream = Memory.Manager.GetStream())
            {
                await using (var streamWriter = new StreamWriter(stream, leaveOpen:true))
                {
                    serializer.Serialize(streamWriter, e);
                    await streamWriter.FlushAsync();
                }

                stream.Seek(0, SeekOrigin.Begin);

                await blob.UploadFromStreamAsync(stream, condition, null, null)
                    .ConfigureAwait(false);
            }
        }
    }
}