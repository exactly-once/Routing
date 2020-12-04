using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExactlyOnce.Routing.Controller.Model.Azure
{
    public class Outbox
    {
        public Dictionary<string, long> Sequences { get; set; }

        [JsonConstructor]
        public Outbox(Dictionary<string, long> sequences)
        {
            Sequences = sequences;
        }

        public Outbox()
            : this(new Dictionary<string, long>())
        {
        }

        public long Stamp(string destinationId)
        {
            if (!Sequences.TryGetValue(destinationId, out var sequence))
            {
                sequence = 0;
                Sequences[destinationId] = sequence;
            }

            Sequences[destinationId]++;

            return sequence;
        }
    }
}