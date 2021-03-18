using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using ExactlyOnce.Routing.ApiContract;
using ExactlyOnce.Routing.Client;
using RouterInstanceInfo = ExactlyOnce.Routing.ApiContract.RouterInstanceInfo;

namespace ExactlyOnce.Routing.CommandLineTool
{
    class RouterDetailsCommand : PrintCommand
    {
        public RouterDetailsCommand() : base("get")
        {
            Add(new Argument<string>("nameOrId"));
            Handler = CommandHandler.Create<InvocationContext, string, string>(async (context, url, nameOrId) =>
            {
                var client = new RoutingControllerClient(url);
                var router = await client.GetRouter(nameOrId);
                if (router == null)
                {
                    return;
                }
                Print(context, GetRouterView(router));
            });
        }

        static IEnumerable<View> GetRouterView(RouterInfo info)
        {
            var nameView = new ContentView(new ContainerSpan(info.Name.Bold(), new ContentSpan(": " + string.Join(", ", info.InterfacesToSites))));
            yield return nameView;
            yield return new ContentView(Environment.NewLine);

            yield return new ContentView("Instances".Bold());

            var destinationsView = new TableView<RouterInstanceInfo>
            {
                Items = info.Instances.Values.ToList()
            };
            destinationsView.AddColumn(i => i.InstanceId, new ContentView("Id".Underline()), ColumnDefinition.Fixed(40));
            destinationsView.AddColumn(i => string.Join(",", i.InterfacesToSites.Select(kvp => $"{kvp.Key}:{kvp.Value}")), new ContentView("Sites".Underline()), ColumnDefinition.Star(1));

            yield return destinationsView;
        }

    }
}