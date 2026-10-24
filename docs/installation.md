# Installation

## Requirements

- Windows x64
- Kitten Space Agency installed
- The [.NET 10 Runtime for Windows x64](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

StarMap is framework-dependent, so the .NET runtime must be installed separately.

## Install StarMap

1. Download the latest ZIP or Windows installer from [StarMap Releases](https://github.com/StarMapLoader/StarMap/releases/latest).
2. For the ZIP, extract it to a folder and run `StarMap.exe` from that folder. For the installer, follow its prompts and select the KSA folder containing `KSA.dll` when asked.
3. If you installed from the ZIP, the first launch creates `StarMapConfig.json` and exits. Open that file, set `GameLocation` as described below, and save it. The installer normally creates this file for you.
4. Run `StarMap.exe` again. With a valid configuration, it starts KSA through StarMap.

Keep `StarMapConfig.json` beside `StarMap.exe` and start StarMap from its installation folder.

## Configure the KSA path

`GameLocation` may be either the KSA installation folder or the full path to `KSA.dll`. If you provide a folder, StarMap looks for `KSA.dll` inside it.

```json
{
  "GameLocation": "C:\\Games\\Kitten Space Agency",
  "RepositoryLocation": "",
  "GameArguments": []
}
```

- `GameLocation` is required.
- `RepositoryLocation` is reserved and currently unused; leave it empty.
- `GameArguments` is optional and passes additional command-line arguments to KSA.

If StarMap reports that it cannot find `KSA.dll`, check the spelling and confirm that `GameLocation` points to the KSA folder or the DLL itself.
