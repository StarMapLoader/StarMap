using StarMap.API.Mediation;

namespace StarMap.API.Services
{
    public interface IMediationService
    {
        ISubscription<T> Subscribe<T>(Action<T> handler) where T : IStarMapEvent;
        IEventPublisher<T> GetPublisher<T>() where T : IStarMapEvent;
    }
}
