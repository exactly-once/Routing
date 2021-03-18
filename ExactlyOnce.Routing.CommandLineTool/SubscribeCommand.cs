using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using ExactlyOnce.Routing.Client;

namespace ExactlyOnce.Routing.CommandLineTool
{
    class SubscribeCommand : Command
    {
        public SubscribeCommand() : base("subscribe")
        {
            Add(new Argument<string>("endpoint")
            {
                Arity = new ArgumentArity(1, 1)
            });
            Add(new Argument<string>("handler")
            {
                Arity = new ArgumentArity(1, 1)
            });
            Add(new Argument<string>("message")
            {
                Arity = new ArgumentArity(1, 1)
            });
            Add(new Argument<string>("replaced-handler")
            {
                Arity = new ArgumentArity(0, 1)
            });
            Add(new Option<string>("--request-id")
            {
                IsRequired = false
            });
            Handler = CommandHandler.Create<InvocationContext, string, string, string, string, string, string>(
                async (context, url, endpoint, handler, message, replacedHandler, requestId) =>
                {
                    var client = new RoutingControllerClient(url);
                    requestId = string.IsNullOrEmpty(requestId)
                        ? Guid.NewGuid().ToString()
                        : requestId;
                    await client.Subscribe(endpoint, handler, replacedHandler, message, requestId);
                });
        }
    }
}