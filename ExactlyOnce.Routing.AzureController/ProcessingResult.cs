using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.AzureController
{
    public class ProcessingResult
    {
        public async Task<IActionResult> Apply(IAsyncCollector<SignalRMessage> signalRMessages)
        {
            foreach (var notification in Notifications)
            {
                await signalRMessages.AddAsync(notification);
            }

            if (Value == null)
            {
                return new StatusCodeResult(StatusCode);
            }
            return new ObjectResult(Value);
        }

        public static ProcessingResult Ok(object value, params SignalRMessage[] notifications)
        {
            return new ProcessingResult(200, value, notifications);
        }

        public static ProcessingResult Status(int statusCode, params SignalRMessage[] notifications)
        {
            return new ProcessingResult(statusCode, null, notifications);
        }

        public ProcessingResult()
        {
        }

        ProcessingResult(int statusCode, object value, SignalRMessage[] notifications)
        {
            StatusCode = statusCode;
            Value = value;
            Notifications = notifications;
        }

        public SignalRMessage[] Notifications { get; set; }
        public int StatusCode { get; set; }

        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        public object Value { get; set; }
    }
}