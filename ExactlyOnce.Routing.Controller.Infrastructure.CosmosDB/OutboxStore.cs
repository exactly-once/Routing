using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.IO;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class OutboxStore : IOutboxStore
    {
        OutboxConfiguration configuration;

        CosmosClient cosmosClient;
        Database database;
        Container container;

        readonly JsonSerializer serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });

        static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        public OutboxStore(CosmosClient cosmosClient, OutboxConfiguration configuration)
        {
            this.cosmosClient = cosmosClient;
            this.configuration = configuration;

            Initialize().GetAwaiter().GetResult();
        }

        public async Task Initialize()
        {
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(configuration.DatabaseId)
                .ConfigureAwait(false);

            container = await database.CreateContainerIfNotExistsAsync(new ContainerProperties
            {
                Id = configuration.ContainerId,
                PartitionKeyPath = "/stateId",
                DefaultTimeToLive = -1 //No expiration unless explicitly set on item level
            }).ConfigureAwait(false);
        }

        public async Task<OutboxItem> Get(string stateId, string id, CancellationToken cancellationToken = default)
        {
            using var response = await container.ReadItemStreamAsync(id, new PartitionKey(stateId), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.ErrorMessage);
            }

            using var streamReader = new StreamReader(response.Content);
            using var jsonTextReader = new JsonTextReader(streamReader);

            return serializer.Deserialize<OutboxItem>(jsonTextReader);
        }


        public async Task Commit(string stateId, string transactionId, CancellationToken cancellationToken = default)
        {
            var outboxItem = await Get(stateId, transactionId, cancellationToken);

            //HINT: outbox item has already been committed
            if (outboxItem == null)
            {
                return;
            }

            outboxItem.Id = outboxItem.RequestId;
            outboxItem.TimeToLiveSeconds = (int)configuration.RetentionPeriod.TotalSeconds;

            var batch = container.CreateTransactionalBatch(new PartitionKey(stateId))
                .DeleteItem(transactionId)
                .UpsertItem(outboxItem);

            var result = await batch.ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            //HINT: it's possible that outbox item has been already committed in which case
            //      item with transactionId doesn't exist anymore
            if (result.IsSuccessStatusCode == false)
            {
                if (result.StatusCode != HttpStatusCode.NotFound)
                {
                    throw new Exception(result.ErrorMessage);
                }
            }
        }

        public async Task Store(OutboxItem outboxItem, CancellationToken cancellationToken = default)
        {
            using (var stream = MemoryStreamManager.GetStream())
            using (var streamWriter = new StreamWriter(stream))
            using (var writer = new JsonTextWriter(streamWriter))
            {
                serializer.Serialize(writer, outboxItem);
                await streamWriter.FlushAsync().ConfigureAwait(false);
                stream.Position = 0;

                var response = await container.UpsertItemStreamAsync(stream, new PartitionKey(outboxItem.StateId), cancellationToken: cancellationToken);

                // HINT: Outbox item should be created or re-updated (if there was a failure
                //       during previous commit).
                if (response.StatusCode != HttpStatusCode.Created &&
                    response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Error storing outbox item");
                }
            }
        }

        public Task Delete(string stateId, string itemId, CancellationToken cancellationToken = default)
        {
            return container.DeleteItemAsync<OutboxItem>(itemId, new PartitionKey(stateId), cancellationToken: cancellationToken);
        }
    }
}