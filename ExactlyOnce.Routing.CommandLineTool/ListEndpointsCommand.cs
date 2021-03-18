using System.CommandLine;
using System.CommandLine.Invocation;
using ExactlyOnce.Routing.Client;

namespace ExactlyOnce.Routing.CommandLineTool
{
    class ListEndpointsCommand : ListCommand
    {
        public ListEndpointsCommand() : base("list")
        {
            Add(new Argument<string>("keyword"));
            Handler = CommandHandler.Create<InvocationContext, string, string>(async (context, url, keyword) =>
            {
                var client = new RoutingControllerClient(url);
                var endpoints = await client.ListEndpoints(keyword);
                Print(context, endpoints);
            });
        }
    }
}