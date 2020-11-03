using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public interface IEventHandler<in T>
        where T : IEvent
    {
        IEnumerable<IEvent> Handle(T e);
    }
}