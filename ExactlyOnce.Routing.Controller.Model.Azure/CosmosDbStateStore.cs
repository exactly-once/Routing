using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class CosmosDbStateStore : IStateStore
    {
        Database database;
        JsonSerializer serializer = new JsonSerializer();

        public CosmosDbStateStore(CosmosClient cosmosClient, string databaseId)
        {
            database = cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId).GetAwaiter().GetResult();
        }

        public async Task<(State, string)> Load(string stateId, Type stateType, CancellationToken cancellationToken = default)
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

        public async Task<string> Upsert(string stateId, State value, string version, CancellationToken cancellationToken = default)
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

                    var response = await container.UpsertItemStreamAsync(
                            payloadStream,
                            new PartitionKey(stateId),
                            requestOptions: new ItemRequestOptions
                            {
                                IfMatchEtag = version,
                            }, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception(response.ErrorMessage);
                    }

                    return response.Headers.ETag;
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