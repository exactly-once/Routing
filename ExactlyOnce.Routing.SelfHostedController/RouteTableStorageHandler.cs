using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.SelfHostedController
{
    public class RouteTableStorageHandler : IMessageHandler
    {
        static readonly JsonSerializer serializer = new JsonSerializer();
        readonly BlobContainerClient blobContainerClient;

        public RouteTableStorageHandler(BlobContainerClient blobContainerClient)
        {
            this.blobContainerClient = blobContainerClient;
        }

        public async Task Handle(EventMessage eventMessage, ISender sender)
        {
            var routingTableChangedEvent = eventMessage.Payload as RoutingTableChanged;
            if (routingTableChangedEvent == null)
            {
                //We only care about that specific event
                return;
            }

            var blobClient = blobContainerClient.GetBlobClient("routing-table.json");

            try
            {
                var props = await blobClient.GetPropertiesAsync();
                await Update(props, routingTableChangedEvent, blobClient).ConfigureAwait(false);
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == 404)
                {
                    await Create(routingTableChangedEvent, blobClient).ConfigureAwait(false);
                }
                else
                {
                    throw new Exception("Error while trying to store routing table snapshot", ex);
                }
            }
        }

        static Task Create(RoutingTableChanged e, BlobClient blobClient)
        {
            var condition = new BlobRequestConditions
            {
                IfNoneMatch = ETag.All
            };
            var metadata = new Dictionary<string, string>
            {
                ["Version"] = e.Version.ToString()
            };
            return Upload(e, metadata, condition, blobClient);
        }

        static async Task Update(Response<BlobProperties> props, RoutingTableChanged e, BlobClient blobClient)
        {
            var version = int.Parse(props.Value.Metadata["Version"]);
            if (e.Version <= version)
            {
                //Do not update to lower version
                return;
            }

            var condition = new BlobRequestConditions {IfMatch = props.Value.ETag};
            await Upload(e, new Dictionary<string, string>(), condition, blobClient).ConfigureAwait(false);
        }

        static async Task Upload(RoutingTableChanged e, Dictionary<string, string> metadata, BlobRequestConditions conditions, BlobClient blob)
        {
            using (var stream = Memory.Manager.GetStream())
            {
                await using (var streamWriter = new StreamWriter(stream, leaveOpen: true))
                {
                    serializer.Serialize(streamWriter, e);
                    await streamWriter.FlushAsync();
                }

                stream.Seek(0, SeekOrigin.Begin);

                await blob.UploadAsync(stream, metadata: metadata, conditions: conditions)
                    .ConfigureAwait(false);
            }
        }
    }
}