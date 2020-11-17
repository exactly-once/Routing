using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using ExactlyOnce.Routing.Endpoint.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace ExactlyOnce.Routing.NServiceBus
{
    class RoutingTableManager : FeatureStartupTask, IRoutingTable
    {
        static readonly ILog log = LogManager.GetLogger<RoutingTable>();

        readonly string routingControllerUrl;
        readonly BlobContainerClient routingControllerBlobContainerClient;
        readonly string routerName;
        readonly string siteName;
        readonly string endpointName;
        readonly string instanceId;
        readonly Dictionary<string, MessageKind> messageKindMap;
        readonly Dictionary<string, string> messageHandlersMap;
        readonly HttpClient httpClient;
        CancellationTokenSource stopTokenSource;
        Task notificationTask;
        Task registerTask;
        volatile RoutingTable table;
        string thisSite;
        TimeSpan httpRetryDelay = TimeSpan.FromSeconds(5);

        public RoutingTableManager(string routingControllerUrl, 
            BlobContainerClient routingControllerBlobContainerClient,
            string routerName,
            string siteName,
            string endpointName, string instanceId, 
            Dictionary<string, MessageKind> messageKindMap, 
            Dictionary<string, string> messageHandlersMap)
        {
            this.routingControllerUrl = routingControllerUrl;
            this.routingControllerBlobContainerClient = routingControllerBlobContainerClient;
            this.routerName = routerName;
            this.siteName = siteName;
            this.endpointName = endpointName;
            this.instanceId = instanceId;
            this.messageKindMap = messageKindMap;
            this.messageHandlersMap = messageHandlersMap;
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(routingControllerUrl)
            };
        }

        protected override async Task OnStart(IMessageSession session)
        {
            stopTokenSource = new CancellationTokenSource();

            var routingTableReady = new TaskCompletionSource<bool>();
            var connected = new TaskCompletionSource<bool>();

            await LoadRoutingTable(routingTableReady).ConfigureAwait(false);

            notificationTask = Task.Run(() => ConnectToNotificationHub(routingTableReady, connected));
            registerTask = Task.Run(async () =>
            {
                try
                {
                    //Only register after we have connected to the hub
                    await connected.Task.ConfigureAwait(false);

                    await Register().ConfigureAwait(false);
                    if (routerName != null)
                    {
                        await SendHelloToRouter().ConfigureAwait(false);
                    }
                    else
                    {
                        await SendHelloToController().ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    //Ignore
                }
            });

            await routingTableReady.Task.ConfigureAwait(false);
            log.Info("Routing table ready.");
        }

        async Task SendHelloToController()
        {
            var reportId = Guid.NewGuid().ToString();
            var payload = new EndpointHelloRequest
            {
                EndpointName = endpointName,
                InstanceId = instanceId,
                ReportId = reportId,
                Site = siteName
            };
            var payloadJson = JsonConvert.SerializeObject(payload);

            while (!stopTokenSource.IsCancellationRequested)
            {
                try
                {
                    var response = await httpClient.PostAsync("api/ProcessEndpointHello", new StringContent(payloadJson));
                    if (response.IsSuccessStatusCode)
                    {
                        log.Info("Endpoint registered its site with the routing controller.");
                        break;
                    }
                    log.Error($"Error while contacting the routing controller. {response.StatusCode}: {response.ReasonPhrase}");
                    await Task.Delay(httpRetryDelay);
                }
                catch (HttpRequestException e)
                {
                    log.Error("Error while contacting the routing controller.", e);
                    await Task.Delay(httpRetryDelay);
                }
            }
        }

        async Task SendHelloToRouter()
        {
            //TODO
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
                throw new Exception("Error while downloading the routing table.", e);
            }

            // ReSharper disable once PossibleNullReferenceException
            var site = routingTable.Sites
                .Where(s => s.Value.Any(i => i.InstanceId == instanceId && i.EndpointName == endpointName))
                .Select(x => x.Key)
                .FirstOrDefault();

            if (site == null)
            {
                log.Warn($"The current routing table (version {routingTable.Version}) does not contain information about this endpoint instance.");
            }
            else
            {
                thisSite = site;
                table = routingTable;
                routingTableReady.SetResult(true);
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
                    log.Info("Endpoint connected to the SignalR notification hub.");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    log.Error("Error while connecting to SignalR notification hub", e);
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
            var site = routingTable.Sites
                .Where(s => s.Value.Any(i => i.InstanceId == instanceId && i.EndpointName == endpointName))
                .Select(x => x.Key)
                .FirstOrDefault();
            
            if (thisSite == null)
            {
                log.Warn($"The updated routing table (version {routingTable.Version}) does not contain information about this endpoint. The update is going to be ignored.");
            }
            else
            {
                table = routingTable;
                thisSite = site;
                routingTableReady.TrySetResult(true);
            }
        }

        async Task Register()
        {
            var reportId = Guid.NewGuid().ToString();
            var payload = new EndpointReportRequest
            {
                EndpointName = endpointName,
                RecognizedMessages = messageKindMap,
                MessageHandlers = messageHandlersMap,
                InstanceId = instanceId,
                ReportId = reportId
            };
            var payloadJson = JsonConvert.SerializeObject(payload);

            while (!stopTokenSource.IsCancellationRequested)
            {
                try
                {
                    var response = await httpClient.PostAsync("api/ProcessEndpointReport", new StringContent(payloadJson));
                    if (response.IsSuccessStatusCode)
                    {
                        log.Info("Endpoint registered with the routing controller.");
                        break;
                    }
                    log.Error($"Error while contacting the routing controller. {response.StatusCode}: {response.ReasonPhrase}");
                    await Task.Delay(httpRetryDelay);

                }
                catch (HttpRequestException e)
                {
                    log.Error("Error while contacting the routing controller.", e);
                    await Task.Delay(httpRetryDelay);
                }
                
            }
        }

        protected override Task OnStop(IMessageSession session)
        {
            stopTokenSource.Cancel();
            return notificationTask;
        }

        public IReadOnlyCollection<RoutingSlip> GetRoutesFor(Type messageType, string explicitDestinationSite)
        {
            var result = table.SelectDestinations(thisSite, messageType.FullName, explicitDestinationSite, new RoutingContext());
            return result;
        }
    }
}