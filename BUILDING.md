# Building PeglinQuickRestart

## Requirements

- Windows with **Peglin** installed (the build references the game's own DLLs, which can't be shared in this repo).
- **BepInEx 5** installed in the game folder ([BepInExPack_Peglin](https://thunderstore.io/c/peglin/p/BepInEx/BepInExPack_Peglin/)).
- **.NET 8 SDK** (`winget install Microsoft.DotNet.SDK.8`).

The project expects the game at the default Steam location,
`C:\Program Files (x86)\Steam\steamapps\common\Peglin`. If yours is elsewhere, add `-p:GamePath="D:\Games\Peglin"`
to the build commands (or `-GamePath` to `pack.ps1`).

## Build

```
dotnet build PeglinQuickRestart.csproj -c Release
```

The build copies the DLL into the game's `BepInEx\plugins\PeglinQuickRestart\` folder. Restart Peglin after building.

## Tests

```
dotnet test tests/PeglinQuickRestart.Tests.csproj
```

The hold timer and the saved run setup (writing and reading `PeglinQuickRestart.lastrun.txt`) live in `src/Core` with no
Unity references, so they can be tested without the game. GitHub Actions runs them on every push
(`.github/workflows/tests.yml`); the mod itself can't be built there because it needs the game's DLLs.

## Release

```
powershell -ExecutionPolicy Bypass -File pack.ps1
```

Checks the versions in `src\Plugin.cs`, `PeglinQuickRestart.csproj`, `thunderstore\manifest.json` and `CHANGELOG.md` agree,
runs the tests, builds Release and writes the Thunderstore package to `dist\PierreMarais-PeglinQuickRestart-<version>.zip`.
