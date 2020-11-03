namespace ExactlyOnce.Routing.Controller.Model
{
    public class DeniedConnection
    {
        public DeniedConnection(string sourceSite, string destinationSite, string router, int deniedByRule)
        {
            SourceSite = sourceSite;
            DestinationSite = destinationSite;
            Router = router;
            DeniedByRule = deniedByRule;
        }

        public string SourceSite { get; }
        public string DestinationSite { get; }
        public string Router { get; }
        public int DeniedByRule { get; set; }
    }
}