# Typed event mediation

`IMediationService` lets mods publish and subscribe to typed events. Event types implement `IStarMapEvent`. Define an event type in an assembly loaded by the publishing mod:

```csharp
using StarMap.API.Mediation;

public sealed record SelectionChanged(string Selection) : IStarMapEvent;
```

## Publishing an event

The mod that owns the event type registers its publisher once, then publishes messages:

```csharp
using StarMap.API;
using StarMap.API.Mediation;
using StarMap.API.Services;

[StarMapMod]
public sealed class SelectionPublisher
{
    private readonly IMediationService _events;
    private IEventPublisher<SelectionChanged>? _publisher;

    public SelectionPublisher(IMediationService events)
    {
        _events = events;
    }

    [StarMapBeforeMain]
    public void Initialize()
    {
        _publisher = _events.RegisterPublisher<SelectionChanged>();
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

Only the mod that owns the event type's assembly can register its publisher, and only one publisher can be registered for each event type. Attempting to register another publisher throws.

## Subscribing to an event

A dependent mod can subscribe after the publisher has registered:

```csharp
using System;
using StarMap.API;
using StarMap.API.Mediation;
using StarMap.API.Services;

[StarMapMod]
public sealed class SelectionSubscriber
{
    private readonly IMediationService _events;
    private ISubscription<SelectionChanged>? _subscription;

    public SelectionSubscriber(IMediationService events)
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

`Subscribe<T>()` throws if a publisher for `T` has not been registered yet. Dispose subscriptions and the publisher when their respective mods unload.

## Synchronous and parallel publishing

`Publish` invokes subscribers synchronously. `PublishParallel` runs each handler concurrently and returns a `Task` that completes when the handlers finish. Handlers used with `PublishParallel` must be thread-safe and should not access game state unless that access is safe from a worker thread.

## Sharing event types between mods

The event type's assembly must be shared with each subscribing mod so both mods use the same event type. Add it to the provider's `ExportedAssemblies` and the consumer's `ImportedAssemblies`. The consumer must also declare the provider as a required dependency if it subscribes during startup. See [mod dependencies](dependencies.md#sharing-event-types-between-mods) for the configuration example.
