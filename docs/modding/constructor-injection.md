# Constructor injection

StarMap creates one instance of each mod's `[StarMapMod]` entry class with `ActivatorUtilities`. Before creating the instance, it creates a dependency-injection scope for that mod and sets the mod context used by the services.

The services currently registered in that scope are:

- `IModServiceRegistry` for registering and retrieving services shared between mods
- `IModMenuService` for adding items to KSA's **Mods** menu
- `IEventBus` for publishing and subscribing to typed messages
- `IPatchService` for registering method patches

## Request services

Declare the services a mod needs as parameters of the entry class's public constructor:

```csharp
using StarMap.API;
using StarMap.API.Services;

[StarMapMod]
public sealed class ExampleMod
{
    private readonly IModMenuService _menu;

    public ExampleMod(IModMenuService menu)
    {
        _menu = menu;
    }
}
```

Use one public constructor, and ensure its parameters can be resolved from the services StarMap registers. There is no public API for a mod to register its own services.

Constructor injection applies only to the `[StarMapMod]` entry class. Lifecycle callbacks are called with their documented fixed signatures; StarMap does not inject their parameters. See [lifecycle callbacks](lifecycle.md).

Each mod has its own dependency-injection scope. Keep injected services on the entry class instance, and dispose handles returned by services, such as menu items, event publishers, and event subscriptions, in `[StarMapUnload]`. For cross-mod service contracts and registration, see [mod-provided services](mod-services.md).
