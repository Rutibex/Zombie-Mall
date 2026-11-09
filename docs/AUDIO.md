# Music and audio

Zombie Mall includes three original synthesized audio cues in `src/Ashen.Game/Assets`: `music_title.wav`, `music_explore.wav`, and `music_combat.wav`. These are PCM WAV assets (not recordings of a live orchestra). They are intentional shipped runtime files, not disposable build artifacts.

The game uses the Playroma audio/music components for playback and additional generated sounds. Those libraries remain separately MIT licensed; see [licensing information](../licenses/ATTRIBUTION.md).

To regenerate the original music, install Python 3 and NumPy, then run:

```sh
python -m pip install numpy
python tools/generate_score.py
```

This replaces the committed WAVs. Listen to and review resulting audio before committing any changes. Environment graphics can be regenerated using `tools/build_environment_33b.py` (which also runs its foundation builder) with Python/Pillow. No Python dependency is required for ordinary gameplay or source builds.
