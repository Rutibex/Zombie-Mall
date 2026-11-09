# Campaign data and validation

Zombie Mall's authored content is stored in `content/campaigns/*.json`. These JSON documents are copied to the publish output under `Campaigns/` and must remain alongside `ZombieMall.exe`. They describe the opening fixture and successive playable campaign definitions and expansions. The current finale campaign is `zombie-mall.finale.json`; earlier definitions remain bundled for compatibility and regression coverage.

The implementation of schemas and import validation lives in `src/Ashen.Core`. The latest campaign schema used by version 0.33.2 is **30**. Campaign data includes connected areas and entrances, actors, items, combat encounters, objectives, dialogue, expedition and survival systems, faction and conclusion data. Serialized IDs are significant to saves; treat edits and migrations carefully.

From the repository root, validate the default content via:

```sh
dotnet run --project src/Ashen.Game/Ashen.Game.csproj -c Release -- --campaign-check
```

For a specific file, pass an absolute path after `--campaign-check`. The regression runner is `dotnet run --project tests/Ashen.Tests/Ashen.Tests.csproj -c Release`. Use these together with an in-game playtest when changing campaigns.

Do not distribute intermediate audit ledgers, staged verification saves, temporary `.*.json.<suffix>` files, or local developer screenshots. All finalized campaign JSON files needed by the application are included in the repository and portable ZIP.
