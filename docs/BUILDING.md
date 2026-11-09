# Building and testing Zombie Mall

## Requirements

- .NET **10 SDK** and network access to NuGet for first restore
- MonoGame DesktopGL (the project references `MonoGame.Framework.DesktopGL` 3.8.5)
- Windows x64 + working OpenGL driver for the packaged target
- Other desktop platforms may also need SDL, OpenAL and OpenGL native dependencies

## Build and run from source

```sh
dotnet restore ZombieMall.sln
dotnet build ZombieMall.sln -c Release
dotnet run --project src/Ashen.Game/Ashen.Game.csproj -c Release
```

On Windows, run `Play.bat` (publishes first if necessary) or explicitly run `Build-Windows.bat` and then `Windows\ZombieMall.exe`.

## Regression checks

```sh
dotnet run --project tests/Ashen.Tests/Ashen.Tests.csproj -c Release
```

This is a standalone **headless C# regression runner**. It is not an xUnit/NUnit suite. `Run-Tests.bat` executes the runner on Windows. Gameplay UI checks and native Windows launch still require manual testing.

The game also has optional diagnostic flags:

```sh
dotnet run --project src/Ashen.Game/Ashen.Game.csproj -c Release -- --audio-check
dotnet run --project src/Ashen.Game/Ashen.Game.csproj -c Release -- --campaign-check
```

## Publish Windows x64

From Windows:

```powershell
.\Build-Windows.bat
```

The script builds a self-contained single-file **exe with external assets**, then calls `packaging/Prepare-Windows.ps1` to verify runtime assets, remove debug symbols/XML build documentation, and copy notices and player documentation into `Windows/`. Do not ship only the `.exe`.

The release workflow (`.github/workflows/release.yml`) performs the same preparation and creates a portable ZIP when you push a version tag matching `v*`.

## Optional art/music regeneration

Generated assets are committed so users **do not** need Python to build or play. Optional generator scripts remain in `tools/` for environment textures, font atlases and original audio. Typical dependencies are Python + Pillow for environments, Python + NumPy for audio. Avoid overwriting assets without reviewing the output and licensing.
