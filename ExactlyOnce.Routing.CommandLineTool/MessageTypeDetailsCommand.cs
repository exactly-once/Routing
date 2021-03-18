using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering.Views;
using ExactlyOnce.Routing.ApiContract;
using ExactlyOnce.Routing.Client;

namespace ExactlyOnce.Routing.CommandLineTool
{
    class MessageTypeDetailsCommand : PrintCommand
    {
        public MessageTypeDetailsCommand() : base("get")
        {
            Add(new Argument<string>("nameOrId"));
            Handler = CommandHandler.Create<InvocationContext, string, string>(async (context, url, nameOrId) =>
            {
                var client = new RoutingControllerClient(url);
                var endpoint = await client.GetMessageType(nameOrId);
                if (endpoint == null)
                {
                    return;
                }
                Print(context, GetMessageTypeView(endpoint));
            });
        }

        static IEnumerable<View> GetMessageTypeView(MessageRoutingInfo info)
        {
            var nameView = new ContentView(info.MessageType.Bold());
            yield return nameView;
            yield return new ContentView(Environment.NewLine);

            yield return new ContentView("Destinations".Bold());

            var destinationsView = new TableView<DestinationInfo>
            {
                Items = info.Destinations
            };
            destinationsView.AddColumn(i => i.Active, new ContentView("Active".Underline()), ColumnDefinition.Fixed(7));
            destinationsView.AddColumn(i => i.EndpointName, new ContentView("Endpoint".Underline()), ColumnDefinition.Fixed(20));
            destinationsView.AddColumn(i => i.HandlerType, new ContentView("Handler".Underline()), ColumnDefinition.Fixed(40));
            destinationsView.AddColumn(i => string.Join(",", i.Sites), new ContentView("Sites".Underline()), ColumnDefinition.Star(1));

            yield return destinationsView;
        }

    }
}