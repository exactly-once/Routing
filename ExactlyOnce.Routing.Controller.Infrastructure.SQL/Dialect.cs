namespace ExactlyOnce.Routing.Controller.Infrastructure.SQL
{
    public abstract class Dialect
    {
        public abstract string StateLoadQuery { get; }
        public abstract string StateInsertCommand { get; }
        public abstract string StateUpdateCommand { get; }
        public abstract string StateListQuery { get; }
    }
}