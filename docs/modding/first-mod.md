# Create your first mod

This walkthrough builds a small mod that logs a message before KSA starts. It uses a lifecycle callback and does not require constructor injection.

## Create a project

Install the .NET 10 SDK, then create a class library and add the StarMap API package:

```powershell
dotnet new classlib --framework net10.0 --name HelloStarMap
cd HelloStarMap
dotnet add package StarMap.API
```

Use the StarMap.API package version compatible with the loader you are targeting. It may have a different version number from the StarMap application. Add `<PlatformTarget>x64</PlatformTarget>` to the project's main `<PropertyGroup>`:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <PlatformTarget>x64</PlatformTarget>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

The project name produces `HelloStarMap.dll` by default, which will match the entry assembly name below.

## Add the entry class

Replace the generated class with:

```csharp
using StarMap.API;

[StarMapMod]
public sealed class HelloStarMap
{
    [StarMapBeforeMain]
    public void Initialize()
    {
        Console.WriteLine("Hello from my mod.");
    }
}
```

This entry class uses a parameterless constructor. For constructor injection and the services registered by StarMap, see the [constructor-injection guide](constructor-injection.md).

## Add `mod.toml`

Create `mod.toml` in the project root:

```toml
name = "HelloStarMap"

[StarMap]
EntryAssembly = "HelloStarMap"
```

`name` is the KSA mod ID. Keep it the same as the mod folder and KSA manifest ID. `EntryAssembly` is the assembly name without `.dll`.

## Build and install

Build the project:

```powershell
dotnet build -c Release
```

Copy `mod.toml`, the built `HelloStarMap.dll`, its `.deps.json`, and any private dependencies from `bin\Release\net10.0\` into a folder named `HelloStarMap` in either [supported mod location](../using-starmap.md#install-and-enable-a-mod). Keep `mod.toml` beside the DLL. Enable the matching mod ID in KSA's manifest, then start the game with `StarMap.exe`.

When loaded, the mod writes its message to the StarMap console before KSA starts.
