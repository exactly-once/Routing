
namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class RouterState : EventDrivenState<Router>
    {
        public RouterState(string routerName)
            : 
        {
        }

        public RouterState(Router router, Inbox inbox, Outbox outbox)
            : base(inbox, outbox, router, router.Name)
        {
            Router = router;
        }

        public Router Router { get; }
    }
}