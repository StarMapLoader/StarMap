# Lifecycle callbacks and services

## Entry point

Mark the entry class with `[StarMapMod]`. StarMap creates one instance for the mod and scans its methods for lifecycle attributes. Use public instance methods with the signatures shown below.

| Attribute | Required method signature | Called |
| --- | --- | --- |
| `[StarMapBeforeMain]` | `void Method()` | After the mod is initialized and before KSA starts. |
| `[StarMapImmediateLoad]` | `void Method(KSA.Mod definingMod)` | Immediately before KSA prepares that mod's systems. |
| `[StarMapAllModsLoaded]` | `void Method()` | After KSA's `ModLibrary.LoadAll` completes. |
| `[StarMapUnload]` | `void Method()` | When KSA unloads. Dispose mod-owned subscriptions and menu items here. |
| `[StarMapBeforeGui]` | `void Method(double dt)` | Before KSA's GUI frame callback. |
| `[StarMapAfterGui]` | `void Method(double dt)` | After KSA's GUI drawing pass. |
| `[StarMapAfterOnFrame]` | `void Method(double currentPlayerTime, double dtPlayer)` | After KSA's `Program.OnFrame`. |

The callback methods return `void`. Use the exact parameter types and counts; methods with a different signature are ignored.

## Constructor-injected services

StarMap provides `IModMenuService`, `IMediationService`, and `IPatchService` through the entry class constructor. See [constructor injection](constructor-injection.md) for the injection mechanism and the individual pages for [menu items](mod-menu.md), [event mediation](event-mediation.md), and [method patching](patching.md) for their usage.
