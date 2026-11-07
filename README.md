# StarMap

[简体中文](README.zh-CN.md)

An arbitrary code mod loader for Kitten Space Agency.

## Documentation

The [StarMap documentation](https://starmaploader.github.io/StarMap/) covers installation, usage, and mod development. Its Markdown source is in [`docs`](docs/).

## Releases

The documentation site tracks the current release; older releases do not have separate documentation snapshots. The current release-candidate documentation is available at [the RC docs site](https://starmaploader.github.io/StarMap/rc/).

## Installation

- Download the ZIP or Windows installer from [Releases](https://github.com/StarMapLoader/StarMap/releases/latest).
- Install the [.NET 10 Runtime for Windows x64](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) if it is not already installed.
- With the installer, select the KSA folder containing `KSA.dll`; it creates `StarMapConfig.json` for you. With the ZIP, extract it, run `StarMap.exe` once to create the config, then set `GameLocation` to the KSA folder or `KSA.dll` path. `RepositoryLocation` can stay empty.
- Keep the config beside `StarMap.exe` and launch StarMap from its installation folder to start KSA with enabled mods.

See the full [installation guide](https://starmaploader.github.io/StarMap/installation/) for details.

## Mod location

Mods can be placed in either of these two locations:

-   `Documents/My Games/Kitten Space Agency/mods/<ModName>/` (recommended, persists across game updates)
-   `<KSA Installation>/Content/<ModName>/`

StarMap uses the game's manifest system to discover mods from both locations.

## Mod creation

For more information on mod creation, check out the example mods: [StarMap-ExampleMods](https://github.com/StarMapLoader/StarMap-ExampleMods).

## Future plans

The long-term goal is an in-game mod manager that can select mods from a remote catalog and install them after a restart. StarMap currently loads local mods only. The planned catalog would index mod IDs, versions, and download URLs; mod files could be hosted elsewhere, such as GitHub Releases.

## Credits

-   Lexi - [KSALoader](https://github.com/cheese3660/KsaLoader)
