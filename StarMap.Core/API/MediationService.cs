using StarMap.API.Mediation;
using StarMap.API.Services;
using StarMap.Core.ModRepository;
using StarMap.Core.Services;

namespace StarMap.Core.API
{
    internal class MediationService : IMediationService
    {
        private readonly RuntimeMod _mod;
        private readonly IMediator _mediator;

        public MediationService(IModContext modContext, IMediator mediator)
        {
            _mod = modContext.Mod;
            _mediator = mediator;
        }

        public IEventPublisher<T> GetPublisher<T>() where T : IStarMapEvent
        {
            return _mediator.RegisterPublisher<T>(_mod);
        }

        public ISubscription<T> Subscribe<T>(Action<T> handler) where T : IStarMapEvent
        {
            return _mediator.Subscribe(handler);
        }
    }
}
