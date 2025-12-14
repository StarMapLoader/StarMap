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
-   StarMapAfterOnFrame: Called after KSA calls Program.OnFrame
