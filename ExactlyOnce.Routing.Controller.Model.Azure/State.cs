using System;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public abstract class State
    {
        protected State(string id)
        {
            Id = id;
        }

        [JsonProperty("id")] public string Id { get; }

        [JsonProperty("_transactionId")] public Guid? TxId { get; internal set; }
    }
}