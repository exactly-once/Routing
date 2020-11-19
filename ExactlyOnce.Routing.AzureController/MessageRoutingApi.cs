using System;
using System.Threading.Tasks;
using ExactlyOnce.Routing.Controller.Model;
using ExactlyOnce.Routing.Controller.Model.Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace ExactlyOnce.Routing.AzureController
{
    public class MessageRoutingApi
    {
        [FunctionName(nameof(Subscribe))]
        public async Task<IActionResult> Subscribe(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] SubscribeRequest request,
            [ExactlyOnce(requestId: "{requestId}", stateId: "{messageType}")] IOnceExecutor<MessageRoutingState, MessageRouting> execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector)
        {

            var messages = await execute.Once(
                x => x.Subscribe(request.HandlerType, request.Endpoint, request.ReplacedHandlerType),
                () => throw new Exception($"Message type not recognized {request.MessageType}"));
            foreach (var eventMessage in messages)
            {
                eventCollector.Add(eventMessage);
            }

            return new OkResult();
        }

        [FunctionName(nameof(Unsubscribe))]
        public async Task<IActionResult> Unsubscribe(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] UnsubscribeRequest request,
            [ExactlyOnce(requestId: "{requestId}", stateId: "{messageType}")] IOnceExecutor<MessageRoutingState, MessageRouting> execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector)
        {

            var messages = await execute.Once(
                x => x.Unsubscribe(request.HandlerType, request.Endpoint),
                () => throw new Exception($"Message type not recognized {request.MessageType}"));
            foreach (var eventMessage in messages)
            {
                eventCollector.Add(eventMessage);
            }

            return new OkResult();
        }

        [FunctionName(nameof(Appoint))]
        public async Task<IActionResult> Appoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] AppointRequest request,
            [ExactlyOnce(requestId: "{requestId}", stateId: "{messageType}")] IOnceExecutor<MessageRoutingState, MessageRouting> execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector)
        {

            var messages = await execute.Once(
                x => x.Appoint(request.HandlerType, request.Endpoint),
                () => throw new Exception($"Message type not recognized {request.MessageType}"));
            foreach (var eventMessage in messages)
            {
                eventCollector.Add(eventMessage);
            }

            return new OkResult();
        }

        [FunctionName(nameof(Dismiss))]
        public async Task<IActionResult> Dismiss(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] DismissRequest request,
            [ExactlyOnce(requestId: "{requestId}", stateId: "{messageType}")] IOnceExecutor<MessageRoutingState, MessageRouting> execute,
            [Queue("event-queue")] ICollector<EventMessage> eventCollector)
        {

            var messages = await execute.Once(
                x => x.Dismiss(request.HandlerType, request.Endpoint),
                () => throw new Exception($"Message type not recognized {request.MessageType}"));
            foreach (var eventMessage in messages)
            {
                eventCollector.Add(eventMessage);
            }

            return new OkResult();
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
}
