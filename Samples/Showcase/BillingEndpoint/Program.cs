using System;
using System.Threading.Tasks;
using SampleInfrastructure;

namespace BillingEndpoint
{
    class Program
    {
        static Task Main(string[] args)
        {
            return EndpointHelper.HostEndpoint("Billing", args);
        }
    }
}
