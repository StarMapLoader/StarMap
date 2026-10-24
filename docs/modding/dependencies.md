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

When using [`IMediationService`](event-mediation.md), the event type must be defined in an assembly loaded by the publishing mod and shared with each subscribing mod. Add that assembly to the provider's `ExportedAssemblies` and to the consumer's `ImportedAssemblies`; make the provider a required dependency if the consumer subscribes during startup.

The consumer also needs a compile-time reference to the provider's event contract assembly. See [typed event mediation](event-mediation.md) for publisher and subscriber examples.
