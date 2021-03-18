using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering.Views;
using System.Linq;
using ExactlyOnce.Routing.ApiContract;
using ExactlyOnce.Routing.Client;

namespace ExactlyOnce.Routing.CommandLineTool
{
    class EndpointDetailsCommand : PrintCommand
    {
        public EndpointDetailsCommand() : base("get")
        {
            Add(new Argument<string>("nameOrId"));
            Handler = CommandHandler.Create<InvocationContext, string, string>(async (context, url, nameOrId) =>
            {
                var client = new RoutingControllerClient(url);
                var endpoint = await client.GetEndpoint(nameOrId);
                if (endpoint == null)
                {
                    return;
                }
                Print(context, GetEndpointView(endpoint));
            });
        }

        static IEnumerable<View> GetEndpointView(EndpointInfo info)
        {
            var nameView = new ContentView(info.Name.Bold());
            yield return nameView;
            yield return new ContentView(Environment.NewLine);

            yield return new ContentView("Message types".Bold());

            var messagesView = new TableView<KeyValuePair<string, MessageKind>>
            {
                Items = info.RecognizedMessages.ToList()
            };
            messagesView.AddColumn(i => i.Value, new ContentView("Kind".Underline()), ColumnDefinition.Fixed(20));
            messagesView.AddColumn(i => i.Key, new ContentView("Name".Underline()), ColumnDefinition.Star(1));

            yield return messagesView;
            yield return new ContentView(Environment.NewLine);

            yield return new ContentView("Instances".Bold());

            var instancesView = new TableView<EndpointInstanceInfo>
            {
                Items = info.Instances.Values.ToList()
            };

            instancesView.AddColumn(i => i.InstanceId, new ContentView("Id".Underline()), ColumnDefinition.Fixed(40));
            instancesView.AddColumn(i => i.InputQueue, new ContentView("Queue".Underline()), ColumnDefinition.Fixed(40));
            instancesView.AddColumn(i => i.Site, new ContentView("Site".Underline()), ColumnDefinition.Star(1));

            yield return instancesView;
        }

    }
}