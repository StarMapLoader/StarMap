# Typed event bus

`IEventBus` lets mods publish and subscribe to typed messages. The bus matches messages by their type; no marker interface is required:

```csharp
public sealed record SelectionChanged(string Selection);
```

## Publishing a message

Inject `IEventBus` into the `[StarMapMod]` entry class and get a publisher for the message type:

```csharp
using StarMap.API;
using StarMap.API.Events;
using StarMap.API.Services;

[StarMapMod]
public sealed class SelectionPublisher
{
    private readonly IEventBus _events;
    private IEventPublisher<SelectionChanged>? _publisher;

    public SelectionPublisher(IEventBus events)
    {
        _events = events;
    }

    [StarMapBeforeMain]
    public void Initialize()
    {
        _publisher = _events.GetPublisher<SelectionChanged>();
    }

    public void ChangeSelection(string selection)
    {
        _publisher?.Publish(new SelectionChanged(selection));
    }

    [StarMapUnload]
    public void Unload()
    {
        _publisher?.Dispose();
    }
}
```

## Subscribing to a message

Subscribe with the same message type. A subscription can be created before any publisher:

```csharp
using System;
using StarMap.API;
using StarMap.API.Events;
using StarMap.API.Services;

[StarMapMod]
public sealed class SelectionSubscriber
{
    private readonly IEventBus _events;
    private ISubscription<SelectionChanged>? _subscription;

    public SelectionSubscriber(IEventBus events)
    {
        _events = events;
    }

    [StarMapBeforeMain]
    public void Initialize()
    {
        _subscription = _events.Subscribe<SelectionChanged>(OnSelectionChanged);
    }

    private void OnSelectionChanged(SelectionChanged message)
    {
        Console.WriteLine($"Selection changed to {message.Selection}");
    }

    [StarMapUnload]
    public void Unload()
    {
        _subscription?.Dispose();
    }
}
```

Keep and dispose publisher and subscription handles when they are no longer needed, usually in `[StarMapUnload]`. Calling `Publish` on a disposed publisher throws `ObjectDisposedException`. The bus does not queue or replay messages sent before a subscription is added.

## Named channels

The overloads with an `eventName` string create a separate channel for that message type. The publisher and subscribers must use the same type and name:

```csharp
_publisher = _events.GetPublisher<SelectionChanged>("selection-panel");
_subscription = _events.Subscribe<SelectionChanged>(
    OnSelectionChanged,
    "selection-panel");
```

Named channels are scoped to the message type; the same name used with a different type is a different channel. Calls without an event name use the default channel and do not communicate with named channels.

## Message delivery

`Publish` invokes the current subscribers synchronously on the calling thread. Handlers for one publish call run one at a time, but their order is not guaranteed. There is no parallel-publish or worker-thread dispatch API. A handler exception propagates from `Publish` and stops delivery for that call. Concurrent publish calls can invoke the same handler concurrently, so protect shared state as needed.

## Sharing message types between mods

Every mod publishing or subscribing to a cross-mod message must use the same runtime type from a shared assembly. Put message contracts in an assembly exported by the mod that provides them and imported by every other mod that uses those types. Each mod using the messages also needs a compile-time reference to the contract assembly. See [mod dependencies](dependencies.md#sharing-event-types-between-mods) for the configuration example.
