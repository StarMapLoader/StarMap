# In-game menu items

`IModMenuService.AddModMenuItem(string label, Action callback)` adds an item to KSA's **Mods** menu. The label must be unique across mods; registering a duplicate label throws. The returned `IDisposable` removes the item.

Inject `IModMenuService` into the `[StarMapMod]` entry class, register the item during initialization, and dispose its handle when the mod unloads:

```csharp
using System;
using StarMap.API;
using StarMap.API.Services;

[StarMapMod]
public sealed class ExampleMod
{
    private readonly IModMenuService _menu;
    private IDisposable? _menuItem;

    public ExampleMod(IModMenuService menu)
    {
        _menu = menu;
    }

    [StarMapBeforeMain]
    public void Initialize()
    {
        _menuItem = _menu.AddModMenuItem(
            "Example action",
            () => Console.WriteLine("Selected from the Mods menu."));
    }

    [StarMapUnload]
    public void Unload()
    {
        _menuItem?.Dispose();
    }
}
```

The callback runs when the player selects the item. For the constructor-injection mechanism, see [constructor injection](constructor-injection.md).
