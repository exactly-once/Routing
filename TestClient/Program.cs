using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

namespace TestClient
{
    class Program
    {
        static HubConnection connection;

        static async Task Main(string[] args)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(args.Length > 0 ? args[0] : "http://localhost:7071/api")
            };

            var commands = new (string, Func<CancellationToken, string[], Task>)[]
            {
                ("e|Send endpoint startup. Syntax: e <report id> <name> <instance> <handlers, comma separated> <messages, comma separated>",
                    (ct, args) => EndpointStartup(args, client)),
                ("h|Send endpoint hello. Syntax: h <report id> <name> <instance> <site>",
                    (ct, args) => EndpointHello(args, client)),
                ("c|Connect to notification service",
                    (ct, args) => Connect(args)),
                ("d|Disconnect from notification service",
                    (ct, args) => Disconnect(args)),
            };

            

            await Run(commands);
        }

        static async Task Disconnect(string[] args)
        {
            if (connection != null)
            {
                await connection.StopAsync();
                connection = null;
            }
        }

        static Task Connect(string[] args)
        {
            if (connection != null)
            {
                return Task.CompletedTask;
            }
            connection = new HubConnectionBuilder().WithUrl("http://localhost:7071/api").Build();
            connection.On<string>("newMessage", x =>
            {
                Console.WriteLine(x);
            });
            return connection.StartAsync(CancellationToken.None);
        }

        static async Task EndpointHello(string[] args, HttpClient httpClient)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Not enough arguments.");
                return;
            }
            var reportId = args[0];
            var name = args[1];
            var instance = args[2];
            var site = args[3];

            var request = new EndpointHelloRequest
            {
                ReportId = reportId,
                EndpointName = name,
                InstanceId = instance,
                Site = site
            };

            var json = JsonConvert.SerializeObject(request);
            var response = await httpClient.PostAsync("api/ProcessEndpointHello", new StringContent(json));
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.ReasonPhrase);
            }
        }

        static async Task EndpointStartup(string[] args, HttpClient httpClient)
        {
            if (args.Length < 5)
            {
                Console.WriteLine("Not enough arguments.");
                return;
            }

            var reportId = args[0];
            var name = args[1];
            var instance = args[2];
            var handlersList = args[3];
            var messagesList = args[4];

            var handlers = handlersList.Split(',').Select(x =>
            {
                var parts = x.Split(':');
                var handlerType = parts[0];
                var messageType = parts[1];
                return (handlerType, messageType);
            }).ToDictionary(x => x.handlerType, x => x.messageType);

            var messages = messagesList.Split(',').Select(x =>
            {
                var parts = x.Split(':');
                var messageType = parts[0];
                var messageKind = (MessageKind) Enum.Parse(typeof(MessageKind), parts[1]);
                return (messageType, messageKind);
            }).ToDictionary(x => x.messageType, x => x.messageKind);

            var request = new EndpointReportRequest
            {
                EndpointName = name,
                InstanceId = instance,
                ReportId = reportId,
                MessageHandlers = handlers,
                RecognizedMessages = messages
            };

            var json = JsonConvert.SerializeObject(request);
            var response = await httpClient.PostAsync("api/ProcessEndpointReport", new StringContent(json));
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.ReasonPhrase);
            }
        }

        static async Task Run((string, Func<CancellationToken, string[], Task>)[] commands)
        {
            Console.WriteLine("Select command:");
            commands.Select(i => i.Item1).ToList().ForEach(Console.WriteLine);

            while (true)
            {
                var commandLine = Console.ReadLine();
                if (commandLine == null)
                {
                    continue;
                }

                var parts = commandLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var key = parts.First().ToLowerInvariant();
                var arguments = parts.Skip(1).ToArray();

                var match = commands.Where(c => c.Item1.StartsWith(key)).ToArray();

                if (match.Any())
                {
                    var command = match.First();

                    Console.WriteLine($"\nExecuting: {command.Item1.Split('|')[1]}");

                    using (var ctSource = new CancellationTokenSource())
                    {
                        Task task;
                        try
                        {
                            task = command.Item2(ctSource.Token, arguments);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            continue;
                        }

                        while (ctSource.IsCancellationRequested == false && task.IsCompleted == false)
                        {
                            if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Enter)
                            {
                                ctSource.Cancel();
                                break;
                            }

                            await Task.Delay(TimeSpan.FromMilliseconds(500));
                        }

                        await task;
                    }

                    Console.WriteLine("Done");
                }
            }
        }
    }
}
