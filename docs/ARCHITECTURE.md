# Project architecture

The `ZombieMall.sln` solution targets .NET 10 / C# 14. The desktop presentation uses MonoGame DesktopGL.

| Path | Role |
| --- | --- |
| `src/Ashen.Core` | Game rules, actors, combat, inventory, progression, persistent campaign state and versioned saves |
| `src/Ashen.Game` | MonoGame window, exploration, rendering, input, UI and asset loading |
| `src/Ashen.Game/Assets` | Artwork, textures, environment metadata, font and sound assets used by the game |
| `src/Playroma.Sound.*`, `src/PlayromaMusic.*` | Separately MIT-licensed audio and music generation components |
| `content/campaigns` | Authored JSON campaign definitions (copied to output under `Campaigns/`) |
| `content/music/styles` | Music style definitions consumed by the audio libraries |
| `tests/Ashen.Tests` | Headless regression executable |
| `tools` | Optional reproducible art, font and score scripts |
| `packaging` | Windows portable build preparation and distribution text |

Keep rules and persistence authoritative in `Ashen.Core` and presentation/UI behavior in `Ashen.Game`. World data is versioned and saves may embed older campaign definitions; changing the external JSON does not automatically rewrite existing saves.

Player profiles are stored in the user profile rather than the repository or extracted game directory. Keep changes to JSON formats, game rules and saves covered by executable regression checks, and preserve third-party attribution when updating assets or dependencies.
