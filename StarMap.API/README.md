# StarMap API

This package provides the API for mods to interface with the [StarMap](https://github.com/StarMapLoader/StarMap) modloader.

## How to create mods

### General architecture

A StarMap mod is in essence an extension of KSA mods.  
Every mod will at minimum contain a mod.toml (which is also the case for KSA mods) as well as an entry assembly.

#### mod.toml

While it is not stricly neccesary, it is advised to add StarMap info to the mod.toml, at its most basic, a mod.toml should like like this:

```toml
name = "MyAmazingMod"

[StarMap]
EntryAssembly = "MyAmazingMod"
```

The name will be the modid of your mod, this should be the same name as the folder in which this lives in the Content folder.  
The "StarMap" section is optional, if it is provided, at mimimum, it should provide the name of the assembly which StarMap will initially load (without the dll extension).
If it is not provided, StarMap will search for a [modid].dll in the mod folder.

#### Entry assembly

The above mentioned entry assembly will be the first thing that StarMap will load.  
Within this assembly, StarMap will search for a class that has the [StarMapMod](#starmapmod) attribute. It will use the first that it finds.
This class will be the core part of the mod, any entry from StarMap will be done through this class.  
To add functionality to the mod, methods with attributes can be added, which attributes are available and what their functionality is, can be found in the [Attributes API reference](#attributes).
These methods will be called as instance methods on the mod class, throughout the runtime of KSA, StarMap will create once instance of this class and reuse it.
The implemenation should follow the signatures shown in the example code.

### Dependencies

In many cases, mods will have dependencies on other mods, or will want to overwrite functionality of other mods.  
To achieve this, some extra configuration is required, this configuration is confined to the mod.toml within the StarMap section.

#### Exported assemblies

First of all, mods can configure what assemblies should be exposed to other mods, by default all assemblies that are provided with the mod can be accessed from other mods, this can be changed with the ExportedAssemblies.  
In below example, only the StarMap.SimpleMod.Dependency(.dll) assembly will be accessable from other mods (more info in [imported and exported assemblies](#imported-and-exported-assemblies)).

```toml
name = "MyOtherAmazingMod"

[StarMap]
EntryAssembly = "MyOtherAmazingMod"
ExportedAssemblies = [
	"MyDependency"
]
```

#### Mod dependency

Then, mods can define what mods they want to depend on, they can do this by adding a new ModDependencies list entry in the mod.toml

```toml
name = "MyAmazingMod"

[StarMap]
EntryAssembly = "MyAmazingMod"

[[StarMap.ModDependencies]]
ModId = "MyOtherAmazingMod"
Optional = false
ImportedAssemblies = [
    "MyDependency"
]
```

In above example, it is provided that StarMap.SimpleMod wants to depend on StarMap.SimpleMod2, this dependency is not optional and the mod wants to access the StarMap.SimpleMod.Dependency assembly.  
Following fields can be used

-   The `ModId` should be the same as is provided as the name field in the mod.toml of the dependency mod.
-   The `Optional` field (default false) defines if this dependency is optional, more info in the [loading strategy](#dependency-loading-strategy)
-   The `ImportedAssemblies` field contains a list of assemblies that this mod intends to use from the dependency (more info in [imported and exported assemblies](#imported-and-exported-assemblies)).

#### Imported and exported assemblies

The goal of the imported and exported assembly fields is to compile a list of assemblies that will be provided to a mod from a dependency, below is the behaviour depending on the content of both fields:

-   If both fields are not filled in, the list will contain the entry assembly of the dependency.
-   If only 1 of the lists is filled in, it will use this list to provide the assemblies.
-   If both lists are defined, the intersection of the two will be used.

## Mod loading strategy

When StarMap is started, it will start with loading the manifest.toml (in the same way KSA does it, only sooner), it will then start loading mods from top to bottom.
It will first load the mod.toml, if the file does not exists, mod loading will not work, if there is no StarMap section, it will use a default configuration.  
Using the config, if there are dependencies, it will first check if these dependencies are already loaded. If they are all loaded, mod loading continues,
otherwise, it stores what dependencies are still needed and continues to the next mod.
It will then search for the entry assembly, if it does not exists, loading will be stopped, otherwise, it will load the assembly. Then will search for a class with the StarMapMod attribute and create an instance. With the instance, it goes over the known attributes and stores a reference to the methods, allowing for quick quering, it stores the StarMapBeforeMain and StarMapImmediateLoad methods seperatly because they are mod specific.  
Once the mod is fully loaded, it will call the StarMapBeforeMain method, if there is any.

Now that the mod has been loaded, it checks the list of mods that are waiting for dependencies, and if there are any that are waiting for this mod. If so, it removes itself from the waiting dependencies and checks if the mod can now be loaded, if so, the mod is loaded and the StarMapBeforeMain of that mod is called.

It does this for all the mods in the manifest.  
Once it has tried loading all the mods, it gets the mods that are still waiting and checks them again.
If for a waiting mod, all its dependencies are optional, it will now load this mod. The implementation of the mod should ensure it can handle the optional dependency can be absent.  
It keeps looping over the list of waiting mods until it has gone through the list once without being able to load a new mod, this indicates there are no more mods that can load with the provided mods, and gives up on loading these mods.

Now StarMap will start KSA, which in turn will call StarMapImmediateLoad for each mod, if implemented.

## Examples

Some examples can be found in the [example mods repository](https://github.com/StarMapLoader/StarMap-ExampleMods)

## API reference

### Attributes

#### StarMapMod

Namespace: `StarMap.API`  
Assembly: `StarMap.API`  
Target: Class

Marks the main class for a StarMap mod.
Only attributes on methods within classes marked with this attribute will be considered.

```csharp
[StarMapMod]
public class ModClass
```

#### StarMapBeforeMain

Namespace: `StarMap.API`  
Assembly: `StarMap.API`  
Target: Method

Methods marked with this attribute will be called before KSA is started.

```csharp
[StarMapBeforeMain]
public void ModMethod()
```

#### StarMapImmediateLoad

Namespace: `StarMap.API`  
Assembly: `StarMap.API`  
Target: Method

Methods marked with this attribute will be called immediately when the mod is loaded by KSA.  
It is called before the `KSA.Mod.PrepareSystems` method for each mod

```csharp
[StarMapBeforeMain]
public void ModMethod(KSA.Mod mod)
```

#### StarMapAllModsLoaded

Namespace: `StarMap.API`  
Assembly: `StarMap.API`  
Target: Method

Methods marked with this attribute will be called when all mods are loaded.  
It is called after the `KSA.ModLibrary.LoadAll` method.

```csharp
[StarMapAllModsLoaded]
public void ModMethod()
```

#### StarMapUnload

Namespace: `StarMap.API`  
Assembly: `StarMap.API`  
Target: Method

Methods marked with this attribute will be called when KSA is unloaded

```csharp
[StarMapUnload]
public void ModMethod()
```

#### StarMapBeforeGui

Namespace: `StarMap.API`  
Assembly: `StarMap.API`  
Target: Method

Methods marked with this attribute will be called before KSA starts creating its ImGui interface.  
It is called just before the `KSA.Program.OnDrawUi` method.

```csharp
[StarMapBeforeGui]
public void ModMethod(double dt)
```

#### StarMapAfterGui

Namespace: `StarMap.API`  
Assembly: `StarMap.API`  
Target: Method

Methods marked with this attribute will be called when KSA has finished creating its ImGui interface.  
It is called just after the `KSA.Program.OnDrawUi` method.

```csharp
[StarMapAfterGui]
public void ModMethod(double dt)
```

#### StarMapAfterOnFrame

Namespace: `StarMap.API`  
Assembly: `StarMap.API`  
Target: Method

Methods marked with this attribute will be called after `KSA.Program.OnFrame` is called.

```csharp
[StarMapAfterOnFrame]
public void ModMethod(double currentPlayerTime, double dtPlayer);
```
