namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class EndpointState : EventDrivenState<Endpoint>
    {
        public EndpointState(Endpoint endpoint, Inbox inbox, Outbox outbox)
            : base(inbox, outbox, endpoint, endpoint.Name)
        {
            Endpoint = endpoint;
        }

        public EndpointState(string endpointName) 
            : this(new Endpoint(endpointName), new Inbox(), new Outbox())
        {
        }

        public Endpoint Endpoint { get; }
    }
}
