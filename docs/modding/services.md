# Services and mechanics

StarMap provides core services and a registry for services shared between mods. Entry classes receive these APIs through constructor injection. Each has its own guide:

- [Constructor injection](constructor-injection.md)
- [Mod-provided services](mod-services.md) with `IModServiceRegistry`
- [In-game menu items](mod-menu.md) with `IModMenuService`
- [Typed event bus](event-bus.md) with `IEventBus`
- [Method patching](patching.md) with `IPatchService`

For entry-point method signatures, see [lifecycle callbacks](lifecycle.md). For declaring mod dependencies and sharing assemblies, see [mod dependencies](dependencies.md).
