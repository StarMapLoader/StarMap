using StarMap.API.Events;
using System.Collections.Concurrent;

namespace StarMap.Core.Services
{
    internal interface IMediator
    {
        IEventPublisher<T> GetPublisher<T>();
        IEventPublisher<T> GetPublisher<T>(string eventName);

        ISubscription<T> Subscribe<T>(Action<T> handler);
        ISubscription<T> Subscribe<T>(Action<T> handler, string eventName);
    }

    internal class Mediator : IMediator
    {
        private interface IEventBus { }

        private readonly ConcurrentDictionary<Type, IEventBus> _busses = [];

        private TypeEventBus<T> GetTypeBus<T>()
        {
            var bus = _busses.GetOrAdd(typeof(T), _ => new TypeEventBus<T>());

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

        public ISubscription<T> Subscribe<T>(Action<T> handler)
        {
            return GetTypeBus<T>().Subscribe(handler);
        }

        public ISubscription<T> Subscribe<T>(Action<T> handler, string eventName)
        {
            return GetTypeBus<T>().GetNamedBus(eventName).Subscribe(handler);
        }

        private class TypeEventBus<T>() : EventBus<T>()
        {
            private readonly ConcurrentDictionary<string, NamedEventBus<T>> _namedBusses = [];

            public EventBus<T> GetNamedBus(string eventName)
            {
                return _namedBusses.GetOrAdd(eventName, (_) => new NamedEventBus<T>());
            }
        }

        private class NamedEventBus<T>() : EventBus<T>() { }

        private abstract class EventBus<T>() : IEventBus
        {
            private readonly Lock _lock = new();


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

            public ISubscription<T> Subscribe(Action<T> action)
            {
                lock (_lock)
                {
                    var subHandle = new SubscriptionHandle<T>(this, action);
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
                    sub.Invoke(message);
                }
            }
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
            private readonly EventBus<T> _bus;
            private readonly Action<T> _action;

            public SubscriptionHandle(EventBus<T> bus, Action<T> action)
            {
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
