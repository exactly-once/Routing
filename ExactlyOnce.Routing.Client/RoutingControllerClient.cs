using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

        public async Task<MessageDestinations> GetDestinations(string messageType)
        {
            try
            {
                var response = await httpClient.GetAsync($"Destinations/{messageType}").ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected status code when getting destinations for type {messageType}: {response.StatusCode}: {response.ReasonPhrase}.");
                }

                var contentString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<MessageDestinations>(contentString);

            }
            catch (HttpRequestException e)
            {
                throw new Exception($"Error while getting destinations for type {messageType}.", e);
            }
        }

        public Task RegisterEndpoint(string endpointName, string instanceId, string inputQueue,
            Dictionary<string, MessageKind> recognizedMessages,
            Dictionary<string, string> messageHandlersMap,
            Dictionary<string, string> legacyDestinations,
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
                ReportId = requestId
            };

            return Post("api/ProcessEndpointReport", payload, "registering endpoint instance");
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
            return Post("api/ProcessEndpointHello", payload, "registering endpoint instance's site");
        }

        public Task RegisterLegacyRoute(string fromEndpoint, Type messageType, string toEndpoint, string toQueue,
            string site, string requestId)
        {

        }

        public Task Subscribe(string endpointName, Type handlerType, Type replacedHandlerType, Type messageType,
            string requestId)
        {
            var handlerTypeName = $"{handlerType.FullName}, {handlerType.Assembly.GetName().Name}";
            var replacedHandlerTypeName = replacedHandlerType != null
                ? $"{replacedHandlerType.FullName}, {replacedHandlerType.Assembly.GetName().Name}"
                : null;

            var messageTypeName = messageType.FullName;

            return Subscribe(endpointName, handlerTypeName, replacedHandlerTypeName, messageTypeName, requestId);
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

            return Post("api/Subscribe", request, "subscribing");
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

            return Post("api/Unsubscribe", request, "unsubscribing");
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

            return Post("api/Appoint", request, "appointing");
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

            return Post("api/Dismiss", request, "dismissing");
        }

        async Task Post(string urlSuffix, object payload, string action)
        {
            var payloadJson = JsonConvert.SerializeObject(payload);
            try
            {
                var response = await httpClient.PostAsync(urlSuffix,
                    new StringContent(payloadJson, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected status code when {action}: {response.StatusCode}: {response.ReasonPhrase}.");
                }
            }
            catch (HttpRequestException e)
            {
                throw new Exception($"Error while {action}.", e);
            }
        }

    }
}
