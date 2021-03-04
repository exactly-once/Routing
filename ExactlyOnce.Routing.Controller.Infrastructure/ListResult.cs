namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class ListResult
    {
        public ListResult(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; }
        public string Name { get; }
    }
}