using StarMap.API.Mediation;

namespace StarMap.API.Services
{
    public interface IMediationService
    {
        ISubscription<T> Subscribe<T>(Action<T> handler) where T : IStarMapEvent;
        IEventPublisher<T> RegisterPublisher<T>() where T : IStarMapEvent;
    }
}
