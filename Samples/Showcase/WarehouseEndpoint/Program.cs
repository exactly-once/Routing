using System;
using System.Threading.Tasks;
using SampleInfrastructure;

namespace WarehouseEndpoint
{
    class Program
    {
        static Task Main(string[] args)
        {
            return EndpointHelper.HostEndpoint("Warehouse", args);
        }
    }
}
