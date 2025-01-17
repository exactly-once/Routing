﻿using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class MessageKindChanged : IEvent
    {
        [JsonConstructor]
        public MessageKindChanged(string fullName, MessageKind newKind, string endpoint)
        {
            FullName = fullName;
            NewKind = newKind;
            Endpoint = endpoint;
        }

        public string Endpoint { get; }
        public string FullName { get; }
        public MessageKind NewKind { get; }
    }
}