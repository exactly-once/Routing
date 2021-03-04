using ExactlyOnce.Routing.Controller.Model.Azure;

namespace ExactlyOnce.Routing.Controller.Infrastructure.CosmosDB
{
    public class ListResponse
    {
        public ListResult[] Documents { get; set; }
    }
}