using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model.Azure;

namespace ExactlyOnce.Routing.SelfHostedController
{
    public interface IMessageHandler
    {
        Task Handle(EventMessage eventMessage, ISender sender);
    }
}