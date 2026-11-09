# Third-party credits and license scope

The repository is a **mixed-license distribution**. Do not assume the root CC BY-SA 4.0 license supersedes independently licensed components.

| Files / components | Attribution / terms |
| --- | --- |
| Original Ashen Covenant source, authored game content, audiovisual assets and generated score | Michael Robinson — CC BY-SA 4.0; see `../LICENSE` |
| Some adapted example assets and integration patterns | Michael Robinson, *Malidor Witches Academy* — CC BY-SA 4.0; the earlier sample is a separate game |
| `src/Playroma.Sound.*`, `src/PlayromaMusic.*` | Playroma Sound and Playroma Music — MIT (supplied notices `PlayromaSound-MIT.txt` and `PlayromaMusic-MIT.txt`; retain notices when redistributing) |
| `src/Ashen.Game/Assets/font.png`, font atlas metadata | DejaVu Sans and DejaVu Serif derived glyph atlases — see `DejaVu.txt` |
| MonoGame, SDL, OpenAL, NVorbis, and .NET runtime binary dependencies | Upstream project licenses and notices under `dependencies/`; these files are included for redistributors of compiled binaries |

The soundtrack WAV files are shipped deliberately: they are **game assets**, not temporary compiler output. `tools/generate_score.py` is included for modifying or rebuilding this original score.

The artwork and gameplay are original to this package or covered by the described project reuse. No proprietary game engine or screenshots from referenced commercial CRPGs are included. If new third-party components are added, document their copyright and license here **before** merging.
