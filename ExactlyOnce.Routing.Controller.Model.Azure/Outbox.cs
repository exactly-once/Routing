using System.Collections.Generic;

namespace ExactlyOnce.Routing.Controller.Model
{
    public class Outbox
    {
        Dictionary<string, long> Sequences;

        public Outbox(Dictionary<string, long> sequences)
        {
            Sequences = sequences;
        }

        public Event Stamp(string sourceId, string destinationId, object payload)
        {
            if (!Sequences.TryGetValue(destinationId, out var sequence))
            {
                sequence = 0;
                Sequences[destinationId] = sequence;
            }

            Sequences[destinationId]++;

            return new Event(sourceId, sequence, payload);
        }
    }
}