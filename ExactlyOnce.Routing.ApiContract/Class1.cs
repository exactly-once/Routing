using System;
using System.Collections.Generic;

namespace ExactlyOnce.Routing.ApiContract
{
    public class EndpointReportRequest
    {
        public string ReportId { get; set; }
        public string EndpointName { get; set; }
        public string InputQueue { get; set; }
        public string InstanceId { get; set; }
        public Dictionary<string, string> MessageHandlers { get; set; }
        public Dictionary<string, MessageKind> RecognizedMessages { get; set; }
        public bool AutoSubscribe { get; set; }
    }

    public class EndpointHelloRequest
    {
        public string ReportId { get; set; }
        public string EndpointName { get; set; }
        public string InstanceId { get; set; }
        public string Site { get; set; }
    }

    public enum MessageKind
    {
        Message,
        Command,
        Event,
        Undefined
    }

    public class LegacyDestinationRequest
    {
        public string RequestId { get; set; }
        public string Site { get; set; }
        public string MessageType { get; set; }
        public string SendingEndpointName { get; set; }
        public string DestinationEndpointName { get; set; }
        public string DestinationQueue { get; set; }
    }

    public class MessageDestinations
    {
        public string MessageType { get; set; }
        public List<Destination> Destinations { get; set; }
    }

    public class Destination
    {
        public string EndpointName { get; set; }
        public string HandlerType { get; set; }
        public bool Active { get; set; }
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

    public class RouterReportRequest
    {
        public string ReportId { get; set; }
        public string RouterName { get; set; }
        public string InstanceId { get; set; }
        public Dictionary<string, string> SiteInterfaces { get; set; }
    }

    public class ConfigureEndpointSiteRoutingRequest
    {
        public string RequestId { get; set; }
        public string EndpointName { get; set; }
        public string Policy { get; set; }
    }
}
