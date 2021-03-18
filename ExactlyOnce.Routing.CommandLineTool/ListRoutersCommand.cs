using System.CommandLine;
using System.CommandLine.Invocation;
using ExactlyOnce.Routing.Client;

namespace ExactlyOnce.Routing.CommandLineTool
{
    class ListRoutersCommand : ListCommand
    {
        public ListRoutersCommand() : base("list")
        {
            Add(new Argument<string>("keyword"));
            Handler = CommandHandler.Create<InvocationContext, string, string>(async (context, url, keyword) =>
            {
                var client = new RoutingControllerClient(url);
                var endpoints = await client.ListRouters(keyword);
                Print(context, endpoints);
            });
        }
    }
}