namespace ExactlyOnce.Routing.Controller.Model
{
    public class Event
    {
        public Event(string source, long sequence, object payload)
        {
            Source = source;
            Sequence = sequence;
            Payload = payload;
        }

        public object Payload { get; }
        public long Sequence { get; }
        public string Source { get; }
    }
}