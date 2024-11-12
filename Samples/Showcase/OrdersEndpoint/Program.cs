using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NServiceBus;
using SampleInfrastructure;

namespace OrdersEndpoint
{
    class Program
    {
        static Task Main(string[] args)
        {
            return EndpointHelper.HostEndpoint("Orders", args, cfg =>
            {
                var persistence = cfg.UsePersistence<SqlPersistence>();
                persistence.ConnectionBuilder(() => new SqlConnection("data source=(local); initial catalog=routing_showcase; integrated security=true"));
                persistence.SubscriptionSettings().DisableCache();
            });
        }
    }
}
