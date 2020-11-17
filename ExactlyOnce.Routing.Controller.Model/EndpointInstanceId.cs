using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class EndpointInstanceId
    {
        [JsonConstructor]
        public EndpointInstanceId(string endpointName, string instanceId)
        {
            EndpointName = endpointName;
            InstanceId = instanceId;
        }

        public string EndpointName { get; }
        public string InstanceId { get; }
    }
}