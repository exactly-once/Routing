using System;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static async Task Main(string[] args)
    {
        var config = EndpointUtils.PrepareEndpoint("Sender", args);
        var endpoint = await Endpoint.Start(config);

        Console.WriteLine("Press c to send a command or e to publish an event.");
        while (true)
        {
            try
            {
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.C:
                        await endpoint.Send(new MyMessage()).ConfigureAwait(false);
                        break;
                    case ConsoleKey.E:
                        await endpoint.Publish(new MyEvent()).ConfigureAwait(false);
                        break;
                    case ConsoleKey.X:
                        await endpoint.Stop();
                        Environment.Exit(0);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
