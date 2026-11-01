# Mod dependencies

Declare StarMap settings and dependencies in the mod's `mod.toml`. Dependency IDs are KSA mod IDs: they must match the `name` in the dependency's `mod.toml` and its ID in KSA's manifest.

```toml
name = "ConsumerMod"

[StarMap]
EntryAssembly = "ConsumerMod"
ExportedAssemblies = [
    "ConsumerMod.Contracts"
]

[[StarMap.ModDependencies]]
ModId = "ProviderMod"
Optional = false
ImportedAssemblies = [
    "ProviderMod.Contracts"
]
```

- `EntryAssembly` names the entry DLL without the `.dll` extension. If omitted with the `[StarMap]` section, it defaults to the mod ID.
- `ExportedAssemblies` lists assemblies this mod makes available to dependents.
- `ModDependencies` may contain multiple `[[StarMap.ModDependencies]]` tables.
- `ModId` identifies the dependency.
- `Optional` defaults to `false`. A required dependency must be present for the dependent mod to initialize. An optional dependency may be absent, so the mod must tolerate its API not being available.
- `ImportedAssemblies` lists assembly names the dependent mod needs from that dependency.

Use assembly names without the `.dll` suffix. The assemblies must be present in the corresponding mod's output folder.

## Which assemblies are shared?

For each dependency, StarMap chooses usable assemblies as follows:

- If neither `ImportedAssemblies` nor `ExportedAssemblies` is set, only the dependency's entry assembly is shared.
- If only one list is set, that list is used.
- If both lists are set, only their intersection is shared.

This lets a provider expose a small contracts assembly instead of sharing all of its implementation. Declare the provider as a dependency before referencing its types, and mark it non-optional if the consumer requires those types to load.

StarMap loads each mod in a separate assembly load context. Declare inter-mod references explicitly; do not rely on an undeclared mod's assembly being found.

## Sharing event types between mods

When using the [typed event bus](event-bus.md), every mod publishing or subscribing to a cross-mod message must use the same runtime type from a shared assembly. Add the contract assembly to the provider's `ExportedAssemblies` and to each dependent mod's `ImportedAssemblies`. Declare the provider as a dependency when its contract assembly is needed, and make it required if the mod cannot function without that assembly. A subscription does not require a publisher to be created first.

Each participating mod also needs a compile-time reference to the contract assembly. See the [typed event bus guide](event-bus.md) for publisher and subscriber examples.

## Sharing mod service contracts

When a mod provides a service through `IModServiceRegistry`, the service contract must be defined in an assembly loaded by the provider and shared with each consumer. Add the contract assembly to the provider's `ExportedAssemblies` and the consumer's `ImportedAssemblies`, and declare the provider as a required dependency when the consumer needs the service during initialization. The consumer also needs a compile-time reference to the contract assembly. See [mod-provided services](mod-services.md) for examples.
