# Modding overview

A StarMap mod extends the KSA mod format. A basic mod consists of:

1. A KSA manifest entry that enables the mod.
2. A mod folder named for the mod ID, containing `mod.toml`.
3. A .NET 10 class-library assembly named by `EntryAssembly`.
4. A class marked with `[StarMapMod]`.

```text
HelloStarMap/
|-- mod.toml
|-- HelloStarMap.dll
|-- HelloStarMap.deps.json
`-- any private dependency assemblies
```

Start with the [first-mod walkthrough](first-mod.md). Then read about [lifecycle callbacks](lifecycle.md), [services and mechanics](services.md), and [mod dependencies](dependencies.md). The service overview links to separate guides for [constructor injection](constructor-injection.md), [mod-provided services](mod-services.md), [menu items](mod-menu.md), [event mediation](event-mediation.md), and [method patching](patching.md).

## Entry assembly and mod ID

The `name` in `mod.toml` is the KSA mod ID. It must match the mod's folder name and its ID in KSA's manifest. `EntryAssembly` is the assembly name without the `.dll` suffix. If the `[StarMap]` section is omitted, StarMap defaults the entry assembly name to the mod ID; explicitly setting it is recommended.

StarMap creates one instance of the marked entry class for the mod. Keep one `[StarMapMod]` class in the entry assembly so there is no ambiguity about which class StarMap selects.

## Build target

StarMap and `StarMap.API` target .NET 10. Build mods as .NET 10 class libraries for x64, and use the `StarMap.API` NuGet package compatible with the loader version you intend to support.

For additional working examples, see the [StarMap-ExampleMods repository](https://github.com/StarMapLoader/StarMap-ExampleMods). The versioned API documentation here describes the attribute-based entry points used by current StarMap releases.
