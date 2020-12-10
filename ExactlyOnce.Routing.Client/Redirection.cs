using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Client
{
    public class Redirection
    {
        public string FromHandler { get; }
        public string FromEndpoint { get; }
        public string ToHandler { get; }
        public string ToEndpoint { get; }

        [JsonConstructor]
        public Redirection(string fromHandler, string fromEndpoint, string toHandler, string toEndpoint)
        {
            FromHandler = fromHandler;
            FromEndpoint = fromEndpoint;
            ToHandler = toHandler;
            ToEndpoint = toEndpoint;
        }
    }

}