# Changelog

## 0.5.3

- Ensure StarMap is compatible with KSA 2025.11.10.5801

## 0.5.1 and 0.5.2

### Fixed

- Ensure API nuget is released correctly

## 0.5.0

### Added

- **Constructor injection for mods.** StarMap can provide its mod services directly to a mod's `[StarMapMod]` entry class. See [constructor injection](modding/constructor-injection.md).
- **Mod menu items.** Mods can add actions to KSA's **Mods** menu and remove them when they unload. See [in-game menu items](modding/mod-menu.md).
- **Typed event bus.** Mods can publish and subscribe to typed messages, including on named channels. See the [event bus guide](modding/event-bus.md).
- **Cross-mod services.** Mods can register and retrieve services through `IModServiceRegistry`. Existing dependency declarations let mods share service and event contracts across load contexts; see [mod-provided services](modding/mod-services.md) and [dependencies](modding/dependencies.md).
- **Method patching API.** `IPatchService` supports prefix and postfix callbacks for static and instance methods, including methods with return values. See [method patching](modding/patching.md).
- **Per-instance data paths.** Launch StarMap with `-InstancePath <path>` or set `STARMAP_INSTANCE_PATH` to override KSA's documents folder path.
- **Documentation site.** Added [installation](installation.md), [usage](using-starmap.md), and [modding guides](modding/index.md), along with a [Chinese README translation](https://github.com/StarMapLoader/StarMap/blob/main/README.zh-CN.md).

### Changed

- StarMap and `StarMap.API` now target .NET 10 and build as AnyCPU.
- Updated KSA and Brutal package references for KSA version `2026.10.25.5707`.
- Releases are now built from version tags.

### Fixed

- Improved mod dependency loading so dependent mods initialize after their providers, including when optional dependencies are unavailable.
- Patch registrations can be disposed individually and are cleaned up when a mod unloads. Exceptions from mod patch callbacks are logged without preventing other patches from running.
