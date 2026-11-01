using StarMap.API.Events;
using StarMap.Core.ModRepository;
using System.Collections.Concurrent;

namespace StarMap.Core.Services
{
    internal interface IMediator
    {
        IEventPublisher<T> GetPublisher<T>();
        IEventPublisher<T> GetPublisher<T>(string eventName);

        ISubscription<T> Subscribe<T>(RuntimeMod mod, Action<T> handler);
        ISubscription<T> Subscribe<T>(RuntimeMod mod, Action<T> handler, string eventName);
    }

    internal class Mediator(ILogger logger) : IMediator
    {
        private interface IEventBus { }

        private readonly ConcurrentDictionary<Type, IEventBus> _busses = [];

        private TypeEventBus<T> GetTypeBus<T>()
        {
            var bus = _busses.GetOrAdd(typeof(T), _ => new TypeEventBus<T>(logger));

            if (bus is TypeEventBus<T> typeBus)
                return typeBus;

            throw new InvalidOperationException("Something went wrong in bus management");
        }

        public IEventPublisher<T> GetPublisher<T>()
        {
            return GetTypeBus<T>().CreatePublisher();
        }

        public IEventPublisher<T> GetPublisher<T>(string eventName)
        {
            return GetTypeBus<T>().GetNamedBus(eventName).CreatePublisher();
        }

        public ISubscription<T> Subscribe<T>(RuntimeMod mod, Action<T> handler)
        {
            return GetTypeBus<T>().Subscribe(mod, handler);
        }

        public ISubscription<T> Subscribe<T>(RuntimeMod mod, Action<T> handler, string eventName)
        {
            return GetTypeBus<T>().GetNamedBus(eventName).Subscribe(mod, handler);
        }

        private class TypeEventBus<T> : EventBus<T>
        {
            public TypeEventBus(ILogger logger) : base(logger) { }

            private readonly ConcurrentDictionary<string, NamedEventBus<T>> _namedBusses = [];

            public EventBus<T> GetNamedBus(string eventName)
            {
                return _namedBusses.GetOrAdd(eventName, (_) => new NamedEventBus<T>(_logger, eventName));
            }

            protected override string GetBusSpecificExceptionPart()
            {
                return $"event for type {typeof(T)}";
            }
        }

        private class NamedEventBus<T> : EventBus<T>
        {
            private readonly string _eventName;

            public NamedEventBus(ILogger logger, string eventName) : base(logger)
            {
                _eventName = eventName;
            }

            protected override string GetBusSpecificExceptionPart()
            {
                return $"event '{_eventName}' for type {typeof(T)}";
            }
        }

        private abstract class EventBus<T> : IEventBus
        {
            private readonly Lock _lock = new();

            protected readonly ILogger _logger;

            public EventBus(ILogger logger)
            {
                _logger = logger;
            }

            private HashSet<SubscriptionHandle<T>> _subscription = [];
            private HashSet<IEventPublisher<T>> _publishers = [];

            public IEventPublisher<T> CreatePublisher()
            {
                lock (_lock)
                {
                    var publisher = new Publisher<T>(this);
                    _publishers.Add(publisher);
                    return publisher;
                }
            }

            public void RemovePublisher(IEventPublisher<T> publisher)
            {
                lock (_lock)
                {
                    _publishers.Remove(publisher);
                }
            }

            public ISubscription<T> Subscribe(RuntimeMod mod, Action<T> action)
            {
                lock (_lock)
                {
                    var subHandle = new SubscriptionHandle<T>(mod, this, action);
                    _subscription.Add(subHandle);
                    return subHandle;
                }
            }

            public void Unsubscribe(SubscriptionHandle<T> handle)
            {
                lock (_lock)
                {
                    _subscription.Remove(handle);
                }
            }

            public void Publish(T message)
            {
                SubscriptionHandle<T>[] subscriptions;

                lock (_lock)
                {
                    subscriptions = [.. _subscription];
                }

                foreach (var sub in subscriptions)
                {
                    try
                    {
                        sub.Invoke(message);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Mod '{sub.Mod.ModId}' could not handle event from bus: {GetBusSpecificExceptionPart()}\n {ex.Message}");
                    }
                }
            }

            protected abstract string GetBusSpecificExceptionPart();
        }

        private class Publisher<T>(EventBus<T> bus) : IEventPublisher<T>
        {
            private bool _isDisposed;

            public void Publish(T message)
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed), this);
                bus.Publish(message);
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _isDisposed, true))
                    return;

                bus.RemovePublisher(this);
            }
        }

        private class SubscriptionHandle<T> : ISubscription<T>
        {
            public RuntimeMod Mod { get; }
            private readonly EventBus<T> _bus;
            private readonly Action<T> _action;

            public SubscriptionHandle(RuntimeMod mod, EventBus<T> bus, Action<T> action)
            {
                Mod = mod;
                _bus = bus;
                _action = action;
            }

            public void Invoke(T message)
            {
                _action.Invoke(message);
            }

            public void Dispose()
            {
                _bus.Unsubscribe(this);
            }
        }
    }
}
