using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public static class HandlerInvoker
    {
        public static IEnumerable<IEvent> Process(IEvent e, object handler, Type handlerType)
        {
            var payloadType = e.GetType();
            var handleInterfaceType = typeof(IEventHandler<>).MakeGenericType(payloadType);
            var interfaces = handlerType.GetInterfaces();
            if (!interfaces.Contains(handleInterfaceType))
            {
                throw new Exception($"Type {handlerType} cannot handle events of type {payloadType.FullName}");
            }

            var resultEnumerable = (IEnumerable<IEvent>)handleInterfaceType.InvokeMember("Handle",
                BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                null, handler,
                new object[] { e });
            return resultEnumerable;
        }
    }
}