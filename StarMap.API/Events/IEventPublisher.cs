namespace StarMap.API.Events
{
    public interface IEventPublisher<T> : IDisposable
    {
        void Publish(T message);
    }
}
