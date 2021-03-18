using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;

namespace ExactlyOnce.Routing.CommandLineTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Command("endpoint")
                {
                    new ListEndpointsCommand(),
                    new EndpointDetailsCommand()
                },
                new Command("message-type")
                {
                    new ListMessageTypesCommand(),
                    new MessageTypeDetailsCommand(),
                    new AppointCommand(),
                    new DismissCommand(),
                    new SubscribeCommand(),
                    new UnsubscribeCommand()
                },
                new Command("router")
                {
                    new ListRoutersCommand(),
                    new RouterDetailsCommand()
                }
            };

            var urlOption = new Option<string>("--url")
            {
                IsRequired = true
            };
            rootCommand.Add(urlOption);

            var builder = new CommandLineBuilder(rootCommand);
            builder.UseAnsiTerminalWhenAvailable();
            builder.UseDefaults();

            var parser = builder.Build();
            parser.InvokeAsync(args).Wait();
        }
    }
}
