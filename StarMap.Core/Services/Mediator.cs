using StarMap.API.Mediation;
using StarMap.Core.ModRepository;
using System.Collections.Concurrent;
using System.Runtime.Loader;

namespace StarMap.Core.Services
{
    internal interface IMediator
    {
        IEventPublisher<T> RegisterPublisher<T>(RuntimeMod mod) where T : IStarMapEvent;
        ISubscription<T> Subscribe<T>(Action<T> action) where T : IStarMapEvent;
    }

    public interface IEventPublisher { }

    internal class SubscriptionHandle<T> : ISubscription<T> where T : IStarMapEvent
    {
        private readonly EventPublisher<T> _publisher;

        private readonly Action<T> _action;

        public SubscriptionHandle(EventPublisher<T> publisher, Action<T> action)
        {
            _publisher = publisher;
            _action = action;
        }

        public void Invoke(T message)
        {
            _action.Invoke(message);
        }

        public void Dispose()
        {
            _publisher.Unsubscribe(this);
        }
    }

    internal class EventPublisher<T>(Mediator mediator) : IEventPublisher<T>, IEventPublisher where T : IStarMapEvent
    {
        private readonly Lock _lock = new();

        private bool _isDisposed;

        private readonly HashSet<SubscriptionHandle<T>> _handlers = [];

        public ISubscription<T> Subscribe(Action<T> action)
        {
            var handle = new SubscriptionHandle<T>(this, action);

            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_isDisposed, this);
                _handlers.Add(handle);
            }

            return handle;
        }

        public void Unsubscribe(SubscriptionHandle<T> handle)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;

                _handlers.Remove(handle);
            }
        }

        public void Publish(T message)
        {
            foreach (var handler in GetHandlersSnapshot())
            {
                handler.Invoke(message);
            }
        }

        public Task PublishParallel(T message)
        {
            var tasks = new List<Task>();

            foreach (var handler in GetHandlersSnapshot())
            {
                tasks.Add(Task.Run(() => handler.Invoke(message)));
            }

            return Task.WhenAll(tasks);
        }

        private SubscriptionHandle<T>[] GetHandlersSnapshot()
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return [];

                return [.. _handlers];
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                mediator.UnregisterPublisher<T>(this);
                _handlers.Clear();
            }

        }
    }

    internal class Mediator : IMediator
    {
        private readonly ConcurrentDictionary<Type, IEventPublisher> _publishers = [];

        public IEventPublisher<T> RegisterPublisher<T>(RuntimeMod mod) where T : IStarMapEvent
        {
            var type = typeof(T);
            if (AssemblyLoadContext.GetLoadContext(typeof(T).Assembly) != mod.ModAssemblyLoadContext)
                throw new Exception("Events are only allowed to be published by the owner of the event type");

            var publisher = new EventPublisher<T>(this);

            if (!_publishers.TryAdd(type, publisher))
                throw new Exception($"A publisher for type '{typeof(T)}' was already registered");

            return publisher;
        }

        public void UnregisterPublisher<T>(IEventPublisher publisher)
        {
            _publishers.TryRemove(new(typeof(T), publisher));
        }

        public ISubscription<T> Subscribe<T>(Action<T> action) where T : IStarMapEvent
        {
            if (!_publishers.TryGetValue(typeof(T), out var publisher))
            {
                throw new Exception("A publisher for this event was not found");
            }

            if (publisher is not EventPublisher<T> typedPublisher)
                throw new Exception("Something went wrong in the type of the publisher");

            return typedPublisher.Subscribe(action);
        }
    }
}
