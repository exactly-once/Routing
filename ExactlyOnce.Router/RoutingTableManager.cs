using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ExactlyOnce.Routing.Endpoint.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using NServiceBus.Logging;
using IDistributionPolicy = ExactlyOnce.Routing.Endpoint.Model.IDistributionPolicy;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace ExactlyOnce.Router
{
    class RoutingTableManager
    {
        static readonly ILog log = LogManager.GetLogger<RoutingTable>();

        readonly string routingControllerUrl;
        readonly BlobContainerClient routingControllerBlobContainerClient;
        readonly string routerName;
        readonly string instanceId;
        readonly Dictionary<string, Func<IDistributionPolicy>> distributionPolicyFactories;
        readonly HttpClient httpClient;
        CancellationTokenSource stopTokenSource;
        Task notificationTask;
        Task registerTask;
        volatile RouterRoutingTableLogic table;
        TimeSpan httpRetryDelay = TimeSpan.FromSeconds(5);

        public RoutingTableManager(string routingControllerUrl,
            BlobContainerClient routingControllerBlobContainerClient,
            Dictionary<string, Func<IDistributionPolicy>> distributionPolicyFactories,
            string routerName,
            string instanceId)
        {
            this.routingControllerUrl = routingControllerUrl;
            this.routingControllerBlobContainerClient = routingControllerBlobContainerClient;
            this.routerName = routerName;
            this.instanceId = instanceId;
            this.distributionPolicyFactories = distributionPolicyFactories;
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(routingControllerUrl)
            };
        }

        public async Task Start(Dictionary<string, string> siteToQueueMap)
        {
            stopTokenSource = new CancellationTokenSource();

            var routingTableReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await LoadRoutingTable(routingTableReady).ConfigureAwait(false);

            notificationTask = Task.Run(() => ConnectToNotificationHub(routingTableReady, connected));
            registerTask = Task.Run(async () =>
            {
                try
                {
                    //Only register after we have connected to the hub
                    await connected.Task.ConfigureAwait(false);
                    await Register(siteToQueueMap).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    //Ignore
                }
            });

            await routingTableReady.Task.ConfigureAwait(false);
            log.Info("Routing table ready.");
        }

        public async Task SendHelloToController(string reportId, string endpointName, string endpointInstanceId, string endpointSite)
        {
            var payload = new EndpointHelloRequest
            {
                EndpointName = endpointName,
                InstanceId = endpointInstanceId,
                ReportId = reportId,
                Site = endpointSite
            };
            var payloadJson = JsonConvert.SerializeObject(payload);

            try
            {
                var response = await httpClient.PostAsync("api/ProcessEndpointHello", new StringContent(payloadJson));
                if (response.IsSuccessStatusCode)
                {
                    log.Info($"Endpoint {endpointName} registered its site with the routing controller.");
                }
                else
                {
                    throw new Exception($"Error while contacting the routing controller. {response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error while contacting the routing controller.", e);
            }
        }

        async Task LoadRoutingTable(TaskCompletionSource<bool> routingTableReady)
        {
            RoutingTable routingTable;
            try
            {
                var response = await routingControllerBlobContainerClient.GetBlobClient("routing-table.json").DownloadAsync();
                var serializer = new JsonSerializer();

                using var streamReader = new StreamReader(response.Value.Content);
                using var textReader = new JsonTextReader(streamReader);

                routingTable = serializer.Deserialize<RoutingTable>(textReader);
            }
            catch (Exception e)
            {
                log.Warn("Error while downloading the routing table. The router will not start until the routing table is loaded", e);
                return;
            }

            try
            {
                table = new RouterRoutingTableLogic(routingTable, distributionPolicyFactories);
                routingTableReady.SetResult(true);
                log.Info($"Routing table version {routingTable.Version} loaded.");
            }
            catch (Exception e)
            {
                log.Error($"Error loading routing table version {routingTable.Version}.", e);
            }
        }

        async Task ConnectToNotificationHub(TaskCompletionSource<bool> routingTableReady,
            TaskCompletionSource<bool> connected)
        {
            //Only one hub at the same time
            var reconnectBarrier = new SemaphoreSlim(1, 1);
            HubConnection establishedConnection = null;

            while (!stopTokenSource.IsCancellationRequested)
            {
                try
                {
                    await reconnectBarrier.WaitAsync(stopTokenSource.Token).ConfigureAwait(false);

                    var connection = new HubConnectionBuilder()
                        .WithUrl(routingControllerUrl)
                        .WithAutomaticReconnect()
                        .Build();

                    connection.Closed += exception =>
                    {
                        if (!stopTokenSource.IsCancellationRequested)
                        {
                            reconnectBarrier.Release();
                        }

                        return Task.CompletedTask;
                    };

                    connection.On<RoutingTableUpdated>("routeTableUpdated", x =>
                    {
                        var routingTable = JsonConvert.DeserializeObject<RoutingTable>(x.JsonContent);
                        RoutingTableUpdated(routingTable, routingTableReady);
                    });

                    await connection.StartAsync(stopTokenSource.Token).ConfigureAwait(false);
                    establishedConnection = connection;
                    connected.TrySetResult(true);
                    log.Info("Router connected to the SignalR notification hub.");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    log.Warn("Error while connecting to SignalR notification hub", e);
                    reconnectBarrier.Release();
                    await Task.Delay(httpRetryDelay).ConfigureAwait(false);
                }
            }

            if (establishedConnection != null)
            {
                await establishedConnection.StopAsync().ConfigureAwait(false);
            }

            connected.TrySetCanceled();
        }

        void RoutingTableUpdated(RoutingTable routingTable, TaskCompletionSource<bool> routingTableReady)
        {
            try
            {
                table = new RouterRoutingTableLogic(routingTable, distributionPolicyFactories);
                routingTableReady.TrySetResult(true);
                log.Info($"Routing table updated to version {routingTable.Version}");
            }
            catch (Exception e)
            {
                log.Error($"Error loading routing table version {routingTable.Version}.", e);
            }
        }

        async Task Register(Dictionary<string, string> siteToQueueMap)
        {
            var reportId = Guid.NewGuid().ToString();
            var payload = new RouterReportRequest
            {
                RouterName = routerName,
                InstanceId = instanceId,
                ReportId = reportId,
                SiteInterfaces = siteToQueueMap
            };
            var payloadJson = JsonConvert.SerializeObject(payload);

            while (!stopTokenSource.IsCancellationRequested)
            {
                try
                {
                    var response = await httpClient.PostAsync("api/ProcessRouterReport", new StringContent(payloadJson));
                    if (response.IsSuccessStatusCode)
                    {
                        log.Info("Endpoint registered with the routing controller.");
                        break;
                    }
                    log.Warn($"Error while contacting the routing controller. {response.StatusCode}: {response.ReasonPhrase}");
                    await Task.Delay(httpRetryDelay);

                }
                catch (HttpRequestException e)
                {
                    log.Warn("Error while contacting the routing controller.", e);
                    await Task.Delay(httpRetryDelay);
                }
            }
        }

        public async Task Stop()
        {
            stopTokenSource.Cancel();
            await notificationTask.ConfigureAwait(false);
            await registerTask.ConfigureAwait(false);
        }

        public (RoutingSlip, string) GetNextHop(string incomingSite, string destinationHandler, string destinationEndpoint, string destinationSite, Dictionary<string, string> messageHeaders)
        {
            var result = table.GetNextHop(incomingSite, destinationHandler, destinationEndpoint, destinationSite, messageHeaders);
            return result;
        }
    }
}