# Contributing to Zombie Mall

Bug reports, reproducible fixes, accessibility improvements, platform work, game-content improvements and documentation changes are welcome. Open an issue first for substantial changes so work can be coordinated.

1. Use .NET 10 and build `ZombieMall.sln` in Release configuration.
2. Run `dotnet run --project tests/Ashen.Tests/Ashen.Tests.csproj -c Release` (it is a custom regression executable).
3. For visual or audio changes, test in-game, and provide before/after images where appropriate. Explain whether old saves remain compatible.
4. Do not commit build outputs (`bin`, `obj`, `Windows`), personal save files, debug dumps, stage captures, snapshots, or other temporary diagnostics.
5. Document new assets/dependencies and their licences in `licenses/ATTRIBUTION.md`. Do not submit unlicensed third-party material.
6. Include clear reproduction steps and test results in pull requests. Code changes to rules/persistence belong in `Ashen.Core`; the MonoGame project should consume those rules.

The repository is mixed-licence. Original project material remains CC BY-SA 4.0; separately licensed Playroma and dependency code must retain their own licence texts and attribution.
