using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class Connection //Graph edge
    {
        [JsonConstructor]
        public Connection(string sourceSite, string destinationSite, string router)
        {
            SourceSite = sourceSite;
            DestinationSite = destinationSite;
            Router = router;
        }

        public string SourceSite { get; }
        public string DestinationSite { get; }
        public string Router { get; }
    }
}