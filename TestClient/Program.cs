using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Endpoint.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;

namespace TestClient
{
    public class RoutingTableUpdated
    {
        public string JsonContent { get; set; }
    }

    class Program
    {
        static HubConnection connection;
        static Random r = new Random();
        static Regex splitBySpaceAndQuotations = new Regex("[^\\s\"']+|\"([^\"]*)\"|'([^']*)'", RegexOptions.Compiled);

        static async Task Main(string[] args)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(args.Length > 0 ? args[0] : "http://localhost:7071/api")
            };

            var commands = new (string, string, Func<CancellationToken, string[], Task>)[]
            {
                ("startup", "Send endpoint startup. Syntax: startup <report id> <name> <instance> <handlers, comma separated> <messages, comma separated>",
                    (ct, args) => EndpointStartup(args, client)),
                ("hello", "Send endpoint hello. Syntax: hello <report id> <name> <instance> <site>",
                    (ct, args) => EndpointHello(args, client)),
                ("sub", "Subscribe. Syntax: sub <endpoint> <handler> <message> <request id?>",
                    (ct, args) => Subscribe(args, client)),
                ("unsub", "Unsubscribe. Syntax: unsub <endpoint> <handler> <message> <request id?>",
                    (ct, args) => Unsubscribe(args, client)),
                ("app", "Appoint. Syntax: app <endpoint> <handler> <message> <request id?>",
                    (ct, args) => Appoint(args, client)),
                ("dis", "Dismiss. Syntax: dis <endpoint> <handler> <message> <request id?>",
                    (ct, args) => Dismiss(args, client)),
                ("c", "Connect to notification service",
                    (ct, args) => Connect(args)),
                ("d", "Disconnect from notification service",
                    (ct, args) => Disconnect(args)),
            };



            await Run(commands);
        }

        static async Task Subscribe(string[] args, HttpClient client)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Not enough arguments.");
                return;
            }
            var name = args[0];
            var handler = args[1];
            var message = args[2];
            var replacedHandler = args.Length > 3 ? args[3] : null;
            var reportId = args.Length > 4 ? args[4] : RandomId();

            var request = new SubscribeRequest
            {
                RequestId = reportId,
                Endpoint = name,
                HandlerType = handler,
                MessageType = message,
                ReplacedHandlerType = replacedHandler
            };

            var json = JsonConvert.SerializeObject(request);
            var response = await client.PostAsync("api/Subscribe", new StringContent(json));
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.ReasonPhrase);
            }
        }

        static string RandomId()
        {
            // ReSharper disable once StringLiteralTypo
            const string letters = "abcdefghijklmnopqrstuvwxyz";

            return new string(Enumerable.Range(0, 4).Select(i => letters[r.Next(letters.Length)]).ToArray());
        }

        static async Task Unsubscribe(string[] args, HttpClient client)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Not enough arguments.");
                return;
            }
            var name = args[0];
            var handler = args[1];
            var message = args[2];
            var reportId = args.Length > 3 ? args[3] : RandomId();


            var request = new UnsubscribeRequest
            {
                RequestId = reportId,
                Endpoint = name,
                HandlerType = handler,
                MessageType = message
            };

            var json = JsonConvert.SerializeObject(request);
            var response = await client.PostAsync("api/Unsubscribe", new StringContent(json));
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.ReasonPhrase);
            }
        }

        static async Task Appoint(string[] args, HttpClient client)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Not enough arguments.");
                return;
            }
            
            var name = args[0];
            var handler = args[1];
            var message = args[2];
            var reportId = args.Length > 3 ? args[3] : RandomId();

            var request = new AppointRequest
            {
                RequestId = reportId,
                Endpoint = name,
                HandlerType = handler,
                MessageType = message
            };

            var json = JsonConvert.SerializeObject(request);
            var response = await client.PostAsync("api/Appoint", new StringContent(json));
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.ReasonPhrase);
            }
        }

        static async Task Dismiss(string[] args, HttpClient client)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Not enough arguments.");
                return;
            }
            
            var name = args[0];
            var handler = args[1];
            var message = args[2];
            var reportId = args.Length > 3 ? args[3] : RandomId();

            var request = new DismissRequest
            {
                RequestId = reportId,
                Endpoint = name,
                HandlerType = handler,
                MessageType = message
            };

            var json = JsonConvert.SerializeObject(request);
            var response = await client.PostAsync("api/Dismiss", new StringContent(json));
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.ReasonPhrase);
            }
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
            connection.On<RoutingTableUpdated>("routeTableUpdated", x =>
            {
                var table = JsonConvert.DeserializeObject<RoutingTable>(x.JsonContent);
                Console.WriteLine(x.JsonContent);
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
                var messageKind = (MessageKind)Enum.Parse(typeof(MessageKind), parts[1]);
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

        static async Task Run((string, string, Func<CancellationToken, string[], Task>)[] commands)
        {
            Console.WriteLine("Select command:");
            commands.Select(i => $"{i.Item1}|{i.Item2}").ToList().ForEach(Console.WriteLine);

            while (true)
            {
                var commandLine = Console.ReadLine();
                if (commandLine == null)
                {
                    continue;
                }

                var parts = SplitBySpaceAndQuotations(commandLine);
                var key = parts.First().ToLowerInvariant();
                var arguments = parts.Skip(1).ToArray();

                var match = commands.Where(c => c.Item1 == key).ToArray();

                var command = match.FirstOrDefault();
                if (command == default)
                {
                    continue;
                }
                Console.WriteLine($"\nExecuting: {command.Item1}");

                using (var ctSource = new CancellationTokenSource())
                {
                    Task task;
                    try
                    {
                        task = command.Item3(ctSource.Token, arguments);
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

        static string[] SplitBySpaceAndQuotations(string commandLine)
        {
            var matchCollection = splitBySpaceAndQuotations
                .Matches(commandLine);

            return matchCollection
                .SelectMany(x => x.Captures)
                .Select(x => x.Value.Trim('\'','"'))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
        }
    }

    public class SubscribeRequest
    {
        public string RequestId { get; set; }
        public string MessageType { get; set; }
        public string Endpoint { get; set; }
        public string HandlerType { get; set; }
        public string ReplacedHandlerType { get; set; }
    }

    public class UnsubscribeRequest
    {
        public string RequestId { get; set; }
        public string MessageType { get; set; }
        public string Endpoint { get; set; }
        public string HandlerType { get; set; }
    }

    public class AppointRequest
    {
        public string RequestId { get; set; }
        public string MessageType { get; set; }
        public string Endpoint { get; set; }
        public string HandlerType { get; set; }
    }

    public class DismissRequest
    {
        public string RequestId { get; set; }
        public string MessageType { get; set; }
        public string Endpoint { get; set; }
        public string HandlerType { get; set; }
    }
}
