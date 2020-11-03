namespace ExactlyOnce.Routing.Controller.Model
{
    public class VisitedSite
    {
        public VisitedSite(string site, VisitedSite previous)
        {
            Site = site;
            Previous = previous;
        }

        public string Site { get; }
        public VisitedSite Previous { get; }

        public bool HasBeenVisited(string candidate)
        {
            return Site == candidate 
                   || (Previous != null && Previous.HasBeenVisited(candidate));
        }
    }
}