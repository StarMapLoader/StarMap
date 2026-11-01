using StarMap.API.Events;

namespace StarMap.API.Services
{
    public interface IEventBus
    {
        IEventPublisher<T> GetPublisher<T>();
        IEventPublisher<T> GetPublisher<T>(string eventName);

        ISubscription<T> Subscribe<T>(Action<T> handler);
        ISubscription<T> Subscribe<T>(Action<T> handler, string eventName);
    }
}
