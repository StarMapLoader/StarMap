# StarMap

A POC/Prototype arbitrary code modloader for Kitten Space Agency.  
Currently this loader can be ran with this functionality in the background, or as a dumb loader just loading mods.  
It makes use of Assembly Load Contexts to ensure mod dependencies are managed seperatly, reducing conflicts

## Releases

To publish a release, create and push a semantic-version tag such as `1.2.3` on the commit to release. The release workflow builds the release and installer from that commit; tags can point to commits on any branch.

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
It currently does this by using two processes to host the game itself seperately so it can restart  
The idea would be to have the repository just be an index of mods, versions and download locations, and that the download locations themselves are seperate (for example github releases)

## Credits

-   Lexi - [KSALoader](https://github.com/cheese3660/KsaLoader)
