using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Client
{
    public class EndpointInstanceId
    {
        [JsonConstructor]
        public EndpointInstanceId(string endpointName, string instanceId, string inputQueue)
        {
            EndpointName = endpointName;
            InstanceId = instanceId;
            InputQueue = inputQueue;
        }

        public string EndpointName { get; }
        public string InstanceId { get; }
        public string InputQueue { get; }
    }
}