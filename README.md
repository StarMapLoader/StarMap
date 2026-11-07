# StarMap

[简体中文](README.zh-CN.md)

An arbitrary code mod loader for Kitten Space Agency.

## Documentation

The [StarMap documentation](https://starmaploader.github.io/StarMap/) covers installation, usage, and mod development. Its Markdown source is in [`docs`](docs/).

## Releases

The documentation site tracks the current release; older releases do not have separate documentation snapshots. The current release-candidate documentation is available at [the RC docs site](https://starmaploader.github.io/StarMap/rc/).

## Installation

-   Download and unzip release from [Releases](https://github.com/StarMapLoader/StarMap/releases/latest).
-   Install the [.NET 10 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) for Windows x64 if it is not already installed.
-   Run StarMap.exe, this will fail and create a StarMapConfig.json.
-   Open StarMapConfig.json and set the location of your KSA installation.
    -   `GameLocation` should be set to the location where Kitten Space Agency was installed, pointing directly to that folder (e.g. `C:\\games\\Kitten Space Agency\\`)
    -   `RepositoryLocation` can be kept empty
-   Run StarMap.exe again, this should launch KSA and load your mods.

## Mod location

Mods can be placed in either of these two locations:

-   `Documents/My Games/Kitten Space Agency/mods/<ModName>/` (recommended, persists across game updates)
-   `<KSA Installation>/Content/<ModName>/`

StarMap uses the game's manifest system to discover mods from both locations.

## Mod creation

For more information on mod creation, check out the example mods: [StarMap-ExampleMods](https://github.com/StarMapLoader/StarMap-ExampleMods).

## Future plans

The goal is to create a modloader similar to the mod functionality in Factorio, where users can select mods in game taken from a remote repository, and that those mods than get installed after an automatic restart of the game  

## Credits

-   Lexi - [KSALoader](https://github.com/cheese3660/KsaLoader)
