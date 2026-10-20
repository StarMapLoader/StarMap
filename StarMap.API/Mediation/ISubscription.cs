namespace StarMap.API.Mediation
{
    public interface ISubscription<T> : IDisposable where T : IStarMapEvent
    {
    }
}
