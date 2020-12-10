using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
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
        public string InstanceId { get; private set; }
        public string InputQueue { get; private set; }

        public void Update(string inputQueue, string instanceId)
        {
            if (InstanceId != null && instanceId == null)
            {
                //We don't update managed instance with legacy instance
                return;
            }
            InputQueue = inputQueue;
            InstanceId = instanceId;
        }
    }
}