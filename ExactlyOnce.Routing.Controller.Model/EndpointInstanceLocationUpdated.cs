using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class EndpointInstanceLocationUpdated : IEvent
    {
        [JsonConstructor]
        public EndpointInstanceLocationUpdated(string endpoint, string instanceId, string site)
        {
            Endpoint = endpoint;
            InstanceId = instanceId;
            Site = site;
        }

        public string Endpoint { get; }
        public string InstanceId { get; }
        public string Site { get; }
    }
}