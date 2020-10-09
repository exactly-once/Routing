namespace ExactlyOnce.Routing.Controller.Model
{
    public enum DestinationState
    {
        Inactive,
        Active,
        DeadEnd //Handler has been removed from a given endpoint while destination was active
    }
}