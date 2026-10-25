using StarMap.API.Events;
using StarMap.API.Services;
using StarMap.Core.ModRepository;
using StarMap.Core.Services;

namespace StarMap.Core.API
{
    internal class EventBus : IEventBus
    {
        private readonly RuntimeMod _mod;
        private readonly IMediator _mediator;

        public EventBus(IModContext modContext, IMediator mediator)
        {
            _mod = modContext.Mod;
            _mediator = mediator;
        }

        public IEventPublisher<T> GetPublisher<T>()
        {
            return _mediator.GetPublisher<T>();
        }

        public IEventPublisher<T> GetPublisher<T>(string eventName)
        {
            return _mediator.GetPublisher<T>(eventName);
        }

        public ISubscription<T> Subscribe<T>(Action<T> handler)
        {
            return _mediator.Subscribe(handler);
        }

        public ISubscription<T> Subscribe<T>(Action<T> handler, string eventName)
        {
            return _mediator.Subscribe(handler, eventName);
        }
    }
}
