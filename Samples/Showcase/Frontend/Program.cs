using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Contracts;
using NServiceBus;
using SampleInfrastructure;

namespace Frontend
{
    class Program
    {
        static readonly Regex expr = new Regex($"order ([A-Za-z]+) with ({string.Join("|", Enum.GetNames(typeof(ItemType)))})+ to ([A-Za-z, ]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static Task Main(string[] args)
        {

            return EndpointHelper.HostEndpoint("Frontend", args, null, async instance =>
            {
                Console.WriteLine("Press <enter> to place an order");
                while (true)
                {
                    var command = Console.ReadLine();

                    if (string.IsNullOrEmpty(command))
                    {
                        break;
                    }

                    var match = expr.Match(command);
                    if (match.Success)
                    {
                        var orderId = match.Groups[1].Value;
                        var items = match.Groups[2].Captures
                            .Select(c => (ItemType) Enum.Parse(typeof(ItemType), c.Value))
                            .Select(x => new Item
                            {
                                Type = x,
                                Quantity = 1
                            }).ToList();

                        var destination = match.Groups[3].Value;
                        var message = new SubmitOrder
                        {
                            OrderId = orderId,
                            DeliveryAddress = destination,
                            Items = items
                        };
                        await instance.Send(message, new SendOptions());
                        continue;
                    }
                    Console.WriteLine("Unrecognized command.");
                }
            });
        }
    }
}
