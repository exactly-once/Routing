﻿using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class OutboxItem
    {
        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("stateId")] public string StateId { get; set; }

        [JsonProperty("requestId")] public string RequestId { get; set; }

        [JsonProperty("sideEffects")] public string SideEffect { get; set; }

        [JsonProperty(PropertyName = "ttl", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeToLiveSeconds { get; set; }
    }
}