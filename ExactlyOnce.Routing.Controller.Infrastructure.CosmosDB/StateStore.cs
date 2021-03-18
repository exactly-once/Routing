using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Infrastructure.CosmosDB
{
    public class StateStore : IStateStore
    {
        readonly CosmosClient cosmosClient;
        readonly string databaseId;
        Database database;
        readonly JsonSerializer serializer = new JsonSerializer();

        public StateStore(CosmosClient cosmosClient, string databaseId)
        {
            this.cosmosClient = cosmosClient;
            this.databaseId = databaseId;
        }

        public async Task Initialize()
        {
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<ListResult>> List(Type stateType, string keyword, CancellationToken cancellationToken = default)
        {
            Container container = await database
                .DefineContainer(stateType.Name, "/id")
                .CreateIfNotExistsAsync()
                .ConfigureAwait(false);

            var queryDefinition = new QueryDefinition($"SELECT c.id as Id, c.SearchKey as Name FROM c WHERE c.SearchKey like \"%{keyword}%\"");
            var options = new QueryRequestOptions();
            var feedIterator = container.GetItemQueryStreamIterator(
                queryDefinition,
                null,
                options);

            var allResults = new List<ListResult>();

            while (feedIterator.HasMoreResults)
            {
                using (var response = await feedIterator.ReadNextAsync(cancellationToken))
                {
                    using (var streamReader = new StreamReader(response.Content))
                    {
                        using (var textReader = new JsonTextReader(streamReader))
                        {
                            var listResponse = serializer.Deserialize<ListResponse>(textReader);
                            allResults.AddRange(listResponse.Documents);
                        }
                    }
                }
            }

            return allResults;
        }

        public async Task<(State, object)> Load(string stateId, Type stateType, CancellationToken cancellationToken = default)
        {
            Container container = await database
                .DefineContainer(stateType.Name, "/id")
                .CreateIfNotExistsAsync()
                .ConfigureAwait(false);

            using var response = await container.ReadItemStreamAsync(stateId, new PartitionKey(stateId), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var newState = (State)Activator.CreateInstance(stateType);
                newState.Id = stateId;
                return (newState, (string)null);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.ErrorMessage);
            }

            using var streamReader = new StreamReader(response.Content);

            var content = await streamReader.ReadToEndAsync()
                .ConfigureAwait(false); ;

            var state = (State)JsonConvert.DeserializeObject(content, stateType);

            return (state, response.Headers.ETag);
        }

        public async Task<object> Upsert(string stateId, State value, object version, CancellationToken cancellationToken = default)
        {
            Container container = await database
                .DefineContainer(value.GetType().Name, "/id")
                .CreateIfNotExistsAsync()
                .ConfigureAwait(false);

            try
            {
                await using (var payloadStream = new MemoryStream())
                await using (var streamWriter = new StreamWriter(payloadStream))
                {
                    serializer.Serialize(streamWriter, value);
                    await streamWriter.FlushAsync();
                    payloadStream.Seek(0, SeekOrigin.Begin);

                    if (version == null)
                    {
                        var response = await container.CreateItemStreamAsync(
                                payloadStream,
                                new PartitionKey(stateId),
                                cancellationToken: cancellationToken)
                            .ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception(response.ErrorMessage);
                        }

                        return response.Headers.ETag;
                    }
                    else
                    {
                        var response = await container.UpsertItemStreamAsync(
                                payloadStream,
                                new PartitionKey(stateId),
                                requestOptions: new ItemRequestOptions
                                {
                                    IfMatchEtag = (string) version,
                                }, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception(response.ErrorMessage);
                        }

                        return response.Headers.ETag;
                    }
                }

            }
            catch (CosmosException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed ||
                                            e.StatusCode == HttpStatusCode.Conflict)
            {
                throw new OptimisticConcurrencyFailure();
            }
        }
    }
}