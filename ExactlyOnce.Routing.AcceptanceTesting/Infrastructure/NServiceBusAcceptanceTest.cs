using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;

namespace NServiceBus.AcceptanceTests
{
    using System.Linq;
    using System.Threading;
    using AcceptanceTesting.Customization;
    using NUnit.Framework;

    /// <summary>
    /// Base class for all the NSB test that sets up our conventions
    /// </summary>
    public abstract partial class NServiceBusAcceptanceTest
    {
        [SetUp]
        public async Task SetUp()
        {
            var blobClient = new BlobContainerClient("UseDevelopmentStorage=true", "routing-table");
            var endpointUri = "https://localhost:8081";
            var primaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            var cosmosClient = new CosmosClient(endpointUri, primaryKey);

            await blobClient.DeleteBlobIfExistsAsync("routing-table.json");
            await cosmosClient.GetDatabase("RoutingAcceptanceTests").DeleteAsync();

            Conventions.EndpointNamingConvention = t =>
            {
                var classAndEndpoint = t.FullName.Split('.').Last();

                var testName = classAndEndpoint.Split('+').First();

                testName = testName.Replace("When_", "");

                var endpointBuilder = classAndEndpoint.Split('+').Last();

                testName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(testName);

                testName = testName.Replace("_", "");

                return testName + "." + endpointBuilder;
            };
        }
    }
}