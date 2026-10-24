# Using StarMap

Start the game by running `StarMap.exe`, not `KSA.exe`. StarMap reads `StarMapConfig.json`, prepares enabled mods from KSA's manifest, then starts the game.

## Install and enable a mod

Mods are ordinary KSA mods with a `mod.toml` and a StarMap entry assembly. Place each mod in one of these locations:

- `%USERPROFILE%\Documents\My Games\Kitten Space Agency\mods\<ModId>\` — recommended; separate from the game installation.
- `<KSA installation>\Content\<ModId>\`

The folder name and KSA manifest mod ID must match. StarMap discovers mods through KSA's manifest; placing a folder by itself does not add or enable the mod. Enable the mod using KSA's normal manifest/mod-management process. StarMap follows the manifest's mod order and resolves declared dependencies while loading.

See [Create your first mod](modding/first-mod.md) for the required files and [Mod dependencies](modding/dependencies.md) for dependency configuration.

## Custom KSA documents path

By default, KSA uses its usual documents location for local mods. StarMap also supports an instance path override if you keep that data elsewhere:

- Pass `-InstancePath <path>` as a game argument, or
- Set the `STARMAP_INSTANCE_PATH` environment variable.

For example, the `GameArguments` setting can include `["-InstancePath", "D:\\KSAProfiles\\Test"]`.

## Troubleshooting

- **StarMap exits after the first launch:** edit the generated `StarMapConfig.json` and set `GameLocation`.
- **KSA does not start:** confirm the .NET 10 x64 runtime is installed and `GameLocation` resolves to `KSA.dll`.
- **A mod is not loaded:** confirm it is enabled in KSA's manifest, the manifest ID matches the mod folder, and the folder contains `mod.toml` plus the configured entry DLL. Check the StarMap console output for load or dependency errors.
- **A required dependency is missing:** install and enable that mod, then check its ID and any imported assembly names in the dependent mod's `mod.toml`.

StarMap currently loads local mods; it does not download mods from a remote catalog.
