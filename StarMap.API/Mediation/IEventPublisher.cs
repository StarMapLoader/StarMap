namespace StarMap.API.Mediation
{
    public interface IEventPublisher<T> : IDisposable where T : IStarMapEvent
    {
        void Publish(T message);
        Task PublishParallel(T message);
    }
}
