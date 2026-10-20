using StarMap.API.Mediation;
using StarMap.Core.ModRepository;
using System.Collections.Concurrent;
using System.Runtime.Loader;

namespace StarMap.Core.Services
{
    internal interface IMediator
    {
        IEventPublisher<T> GetPublisher<T>(RuntimeMod mod) where T : IStarMapEvent;
        ISubscription<T> Subscribe<T>(Action<T> action) where T : IStarMapEvent;
    }

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

    internal abstract class EventPublisher<T>() : IEventPublisher<T> where T : IStarMapEvent
    {
        protected readonly HashSet<SubscriptionHandle<T>> _handlers = [];

        public ISubscription<T> Subscribe(Action<T> action)
        {
            var handle = new SubscriptionHandle<T>(this, action);

            lock (_handlers)
            {
                _handlers.Add(handle);
            }

            return handle;
        }

        public void Unsubscribe(SubscriptionHandle<T> handle)
        {
            lock (_handlers)
            {
                _handlers.Remove(handle);
            }
        }

        public void Publish(T message)
        {
            PublishInner(message);
        }

        public abstract void PublishInner(T message);
    }

    internal class SerialEventPublisher<T>() : EventPublisher<T> where T : IStarMapEvent
    {
        public override void PublishInner(T message)
        {
            foreach (var handler in _handlers)
            {
                handler.Invoke(message);
            }
        }
    }

    internal class ParallelEventPublisher<T>() : EventPublisher<T> where T : IStarMapEvent
    {
        public override void PublishInner(T message)
        {
            foreach (var handler in _handlers)
            {
                _ = Task.Run(() => handler.Invoke(message));
            }
        }
    }

    internal class Mediator : IMediator
    {
        private readonly ConcurrentDictionary<Type, IEventPublisher> _publishers = [];

        public IEventPublisher<T> GetPublisher<T>(RuntimeMod mod) where T : IStarMapEvent
        {
            var type = typeof(T);
            if (AssemblyLoadContext.GetLoadContext(typeof(T).Assembly) != mod.ModAssemblyLoadContext)
                throw new Exception("Events are only allowed to be published by the owner of the event type");

            EventPublisher<T> publisher;

            if (type.IsAssignableTo(typeof(ISerialStarMapEvent)))
                publisher = new SerialEventPublisher<T>();
            else
                publisher = new ParallelEventPublisher<T>();

            if (!_publishers.TryAdd(type, publisher))
                throw new Exception($"A publisher for type '{typeof(T)}' was already registered");

            return publisher;
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
