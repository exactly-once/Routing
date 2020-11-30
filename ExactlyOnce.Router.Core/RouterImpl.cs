using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExactlyOnce.Router.Core
{
    class RouterImpl : IRouter
    {
        public RouterImpl(string name, Interface[] interfaces, SendOnlyInterface[] sendOnlyInterfaces, RuntimeTypeGenerator runtimeTypeGenerator)
        {
            this.sendOnlyInterfaces = sendOnlyInterfaces;
            this.runtimeTypeGenerator = runtimeTypeGenerator;
            this.interfaces = interfaces;
        }

        public async Task Initialize()
        {
            if (initialized)
            {
                throw new Exception("The router has already been initialized.");
            }

            foreach (var iface in sendOnlyInterfaces)
            {
                var endpoint = await iface.Initialize().ConfigureAwait(false);
                interfaceDictionary[iface.Name] = endpoint;
                
            }

            foreach (var iface in interfaces)
            {
                var endpoint = await iface.Initialize((receivedMessage, incomingInterface) 
                    => new MessageRoutingContext(receivedMessage, incomingInterface, interfaceDictionary, runtimeTypeGenerator)).ConfigureAwait(false);
                interfaceDictionary[iface.Name] = endpoint;
            }

            initialized = true;
        }

        public async Task Start()
        {
            if (!initialized)
            {
                await Initialize().ConfigureAwait(false);
            }

            await Task.WhenAll(interfaces.Select(p => p.StartReceiving())).ConfigureAwait(false);
        }

        public IReadOnlyDictionary<string, IRawEndpoint> Interfaces => interfaceDictionary;

        public async Task Stop()
        {
            await Task.WhenAll(interfaces.Select(s => s.StopReceiving())).ConfigureAwait(false);
            await Task.WhenAll(interfaces.Select(s => s.Stop())).ConfigureAwait(false);
            await Task.WhenAll(sendOnlyInterfaces.Select(s => s.Stop())).ConfigureAwait(false);
        }

        bool initialized;
        readonly Dictionary<string, IRawEndpoint> interfaceDictionary = new Dictionary<string, IRawEndpoint>();
        readonly Interface[] interfaces;
        readonly SendOnlyInterface[] sendOnlyInterfaces;
        readonly RuntimeTypeGenerator runtimeTypeGenerator;
    }
}