# StarMap API

This package provides the API for mods to interface with the [StarMap](https://github.com/StarMapLoader/StarMap) modloader.
The main class of the mod should be marked by the StarMapMod attribute.
Then methods within this class can be marked with any of the StarMapMethod attributes.
At the initialization of the mod within KSA, an instance of the StarMapMod is created, only the first class that has this attribute will be considered.
Any method within this class that has any of the attributes will used, so if two methods use StarMapImmediateLoad, both will be called.

## Attributes

-   StarMapMod: Main attribute to mark the mod class.
-   StarMapImmediateLoad: Called immediatly when the mod is loaded in KSA.
-   StarMapAllModsLoaded: Called once all mods are loaded, can be used when this mod has a dependency on another mod.
-   StarMapUnload: Called when KSA is unloaded.
-   StarMapBeforeGui: Called just before KSA starts drawing its Ui.
-   StarMapAfterGui: Called after KSA has drawn its Ui.

## Dependencies

Mods can define what mods they define on, as well as what assemblies they want to export themselves.
This only falls on assembly loading and load order within StarMap.
If a mod is set as a dependency, and it is present, it will be loaded before the mods that depends on it.
The dependent mod can then access any assembly that the dependency exposes, in addition to the main mod assembly, which is always exposed.

This requires following attributes:

-   StarMapDependenciesAttribute: Describes what mods this mod dependends on.

    -   This attribute should be placed on a static property that returns an array of StarMapDependencyInfo.
    -   The StarMapDependencyInfo provides info on the mod id of the mod that should be depended on, and if this dependency is optional.
    -   When a dependency is not optional, the mod will not be loaded when the dependency is not present.
    -   When a dependency is optional, the mod is loaded after all other mods are loaded, to ensure the dependency can be loaded before.

-   StarMapExportedAssemblyAttribute: Describes what assemblies are exposed by the mod.
    -   This attribute should be placed on a static property that returns an array of strings.
    -   The string should be the name of the assembly, with no ".dll" suffix.
    -   The main mod assembly (modid.dll) is always exposed, so this is only for other assemblies that other mods could depend on.
