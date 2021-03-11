using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ExactlyOnce.Routing.ApiContract;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Client
{
    public class RoutingControllerClient
    {
        readonly HttpClient httpClient;

        public RoutingControllerClient(string baseUrl)
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public Task<ListResponse> ListEndpoints(string keyword)
        {
            return Get<ListResponse>($"ListEndpoints/{keyword}");
        }

        public Task<ListResponse> ListRouters(string keyword)
        {
            return Get<ListResponse>($"ListRouters/{keyword}");
        }

        public Task<ListResponse> ListMessageTypes(string keyword)
        {
            return Get<ListResponse>($"ListMessageTypes/{keyword}");
        }

        public Task<MessageRoutingInfo> GetMessageType(string idOrName)
        {
            return Get<MessageRoutingInfo>($"MessageType/{idOrName}");
        }

        public Task<EndpointInfo> GetEndpoint(string idOrName)
        {
            return Get<EndpointInfo>($"Endpoint/{idOrName}");
        }

        public Task<RouterInfo> GetRouter(string idOrName)
        {
            return Get<RouterInfo>($"Router/{idOrName}");
        }

        public Task<RoutingTable> GetRoutingTable()
        {
            return Get<RoutingTable>("GetRoutingTable");
        }

        public Task RegisterEndpoint(string endpointName, string instanceId, string inputQueue,
            Dictionary<string, MessageKind> recognizedMessages,
            Dictionary<string, string> messageHandlersMap,
            Dictionary<string, string> legacyDestinations,
            bool autoSubscribe,
            string requestId)
        {
            var payload = new EndpointReportRequest
            {
                EndpointName = endpointName,
                InputQueue = inputQueue,
                RecognizedMessages = recognizedMessages,
                MessageHandlers = messageHandlersMap,
                LegacyDestinations = legacyDestinations,
                InstanceId = instanceId,
                AutoSubscribe = autoSubscribe,
                ReportId = requestId
            };

            return Post("api/ProcessEndpointReport", payload);
        }

        public Task RegisterEndpointSite(string endpointName, string instanceId, string siteName, string requestId)
        {
            var payload = new EndpointHelloRequest
            {
                EndpointName = endpointName,
                InstanceId = instanceId,
                ReportId = requestId,
                Site = siteName
            };
            return Post("api/ProcessEndpointHello", payload);
        }

        public Task RegisterLegacyDestination(string sendingEndpoint, string messageType, string destinationEndpoint, string destinationQueue,
            string site, string requestId)
        {
            var payload = new LegacyDestinationRequest
            {
                DestinationEndpointName =  destinationEndpoint,
                DestinationQueue = destinationQueue,
                Site = site,
                MessageType = messageType,
                SendingEndpointName = sendingEndpoint,
                RequestId = requestId
            };

            return Post("api/RegisterLegacyDestination", payload);
        }

        public Task Subscribe(string endpointName, Type handlerType, Type replacedHandlerType, Type messageType,
            string requestId)
        {
            var handlerTypeName = GetHandlerTypeName(handlerType);
            var replacedHandlerTypeName = replacedHandlerType != null
                ? GetHandlerTypeName(replacedHandlerType)
                : null;

            var messageTypeName = messageType.FullName;

            return Subscribe(endpointName, handlerTypeName, replacedHandlerTypeName, messageTypeName, requestId);
        }

        static string GetHandlerTypeName(Type handlerType)
        {
            return $"{handlerType.FullName}, {handlerType.Assembly.GetName().Name}";
        }

        public Task Subscribe(string endpointName, string handlerType, string replacedHandlerType, string messageType, string requestId)
        {
            var request = new SubscribeRequest
            {
                RequestId = requestId,
                Endpoint = endpointName,
                HandlerType = handlerType,
                MessageType = messageType,
                ReplacedHandlerType = replacedHandlerType
            };

            return Post("api/Subscribe", request);
        }

        public Task Unsubscribe(string endpointName, string handlerType, string messageType, string requestId)
        {
            var request = new UnsubscribeRequest
            {
                RequestId = requestId,
                Endpoint = endpointName,
                HandlerType = handlerType,
                MessageType = messageType,
            };

            return Post("api/Unsubscribe", request);
        }

        public Task Appoint(string endpointName, Type handlerType, Type messageType, string requestId)
        {
            var handlerTypeName = $"{handlerType.FullName}, {handlerType.Assembly.GetName().Name}";
            var messageTypeName = messageType.FullName;

            return Appoint(endpointName, handlerTypeName, messageTypeName, requestId);
        }

        public Task Appoint(string endpointName, string handlerType, string messageType, string requestId)
        {
            var request = new AppointRequest
            {
                RequestId = requestId,
                Endpoint = endpointName,
                HandlerType = handlerType,
                MessageType = messageType,
            };

            return Post("api/Appoint", request);
        }

        public Task Dismiss(string endpointName, string handlerType, string messageType, string requestId)
        {
            var request = new DismissRequest
            {
                RequestId = requestId,
                Endpoint = endpointName,
                HandlerType = handlerType,
                MessageType = messageType,
            };

            return Post("api/Dismiss", request);
        }

        async Task Post(string urlSuffix, object payload)
        {
            var payloadJson = JsonConvert.SerializeObject(payload);
            try
            {
                var response = await httpClient.PostAsync(urlSuffix,
                    new StringContent(payloadJson, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected status code when posting to {urlSuffix}: {response.StatusCode}: {response.ReasonPhrase}.");
                }
            }
            catch (HttpRequestException e)
            {
                throw new Exception($"Error while posting to {urlSuffix}.", e);
            }
        }

        public async Task<T> Get<T>(string urlSuffix)
        {
            try
            {
                var response = await httpClient.GetAsync(urlSuffix).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return default;
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected status code when querying {urlSuffix}: {response.StatusCode}: {response.ReasonPhrase}.");
                }

                var contentString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<T>(contentString);

            }
            catch (HttpRequestException e)
            {
                throw new Exception($"Error while querying {urlSuffix}.", e);
            }
        }
    }
}
