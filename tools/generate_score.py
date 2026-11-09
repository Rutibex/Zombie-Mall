"""Render Ashen Covenant's original three cue score as deterministic PCM WAVs.

Requires numpy; run from any directory with `python tools/generate_score.py`.
The source note patterns are original and are intentionally independent of game logic.
"""
from pathlib import Path
import wave
import numpy as np

RATE = 22050
ASSETS = Path(__file__).resolve().parents[1] / "src/Ashen.Game/Assets"
RNG = np.random.default_rng(7319)
CHORDS = {
    "Dm": (38, [50, 53, 57, 62]),
    "Bb": (34, [46, 50, 53, 58]),
    "Gm": (31, [43, 46, 50, 55]),
    "A": (33, [45, 49, 52, 57]),
    "C": (36, [48, 52, 55, 60]),
    "Eb": (39, [51, 55, 58, 63]),
}


def hz(midi):
    return 440.0 * 2 ** ((midi - 69) / 12)


def note(out, start, duration, midi, gain, voice):
    begin = int(start * RATE)
    count = int(duration * RATE)
    if count <= 0:
        return
    t = np.arange(count, dtype=np.float64) / RATE
    frequency = hz(midi)
    vibrato = (0.0017 if voice in ("strings", "horn", "choir") else 0.0005)
    phase = 2 * np.pi * frequency * (t + vibrato * np.sin(t * 2 * np.pi * 5.1))
    if voice == "strings":
        wave = np.sin(phase) + .38 * np.sin(phase * 2 + .22) + .16 * np.sin(phase * 3)
        attack, release = .19, .32
    elif voice == "horn":
        wave = np.sin(phase) + .52 * np.sin(phase * 2) + .26 * np.sin(phase * 3) + .12 * np.sin(phase * 4)
        attack, release = .09, .27
    elif voice == "choir":
        wave = np.sin(phase) + .28 * np.sin(phase * 2 + .3) + .18 * np.sin(phase * .997)
        attack, release = .37, .49
    elif voice == "flute":
        wave = np.sin(phase) + .12 * np.sin(phase * 2) + .05 * np.sin(phase * 3)
        attack, release = .08, .23
    elif voice == "harp":
        wave = np.sin(phase) + .42 * np.sin(phase * 2) + .19 * np.sin(phase * 3)
        attack, release = .007, .2
    else:  # low strings
        wave = np.sin(phase) + .3 * np.sin(phase * 2) + .09 * np.sin(phase * 3)
        attack, release = .08, .21
    envelope = np.minimum(1, t / attack) * np.minimum(1, (duration - t) / release)
    if voice == "harp":
        envelope *= np.exp(-t * 3.0)
    else:
        envelope *= .83 + .17 * np.sin(np.pi * t / duration)
    sample = (wave * envelope * gain).astype(np.float32)
    position = (np.arange(count) + begin) % len(out)
    np.add.at(out, position, sample)


def drum(out, start, gain, bright=False):
    count = int(RATE * (.34 if bright else .65))
    t = np.arange(count) / RATE
    sweep = (170 if bright else 84) * t - (85 if bright else 35) * t * t
    tone = np.sin(2 * np.pi * sweep)
    noise = RNG.normal(0, 1, count)
    noise = np.convolve(noise, np.ones(16 if bright else 44) / (16 if bright else 44), mode="same")
    sample = (tone * .78 + noise * (.24 if bright else .34)) * np.exp(-t * (17 if bright else 7)) * gain
    position = (np.arange(count) + int(start * RATE)) % len(out)
    np.add.at(out, position, sample.astype(np.float32))


def render(name, tempo, progression, melody, kind):
    beat = 60 / tempo
    bars = len(progression)
    out = np.zeros(round(bars * 4 * beat * RATE), dtype=np.float32)
    for bar, chord in enumerate(progression):
        bass, tones = CHORDS[chord]
        start = bar * 4 * beat
        # Slow sustained harmony, arranged in voices rather than block-chord beeps.
        for j, midi in enumerate(tones):
            if kind == "explore" and j == 3 and bar % 2:
                continue
            note(out, start, 4 * beat + .34, midi + (12 if j == 3 else 0),
                 (.023 if kind == "explore" else .029) * (1 if j < 2 else .73), "strings")
        note(out, start, 3.8 * beat, bass, .063 if kind == "combat" else .043, "bass")
        if kind == "title":
            note(out, start + beat * .15, 3.7 * beat, tones[1] + 12, .027, "choir")
            if bar % 4 == 0:
                note(out, start, 3 * beat, bass + 12, .047, "horn")
                drum(out, start, .082)
        elif kind == "explore":
            for step, degree in enumerate((0, 2, 1, 3, 2, 1)):
                note(out, start + (step + .5) * beat * .59, 1.3 * beat, tones[degree] + 12,
                     .023 if bar % 4 != 3 else .015, "harp")
            if bar % 4 == 0:
                drum(out, start, .025)
        else:
            for i in range(8):
                note(out, start + i * beat / 2, beat * .38, tones[i % 3] + (0 if i % 2 else 12), .029, "strings")
                if i % 2 == 0:
                    drum(out, start + i * beat / 2, .105 if i == 0 else .065)
                else:
                    drum(out, start + i * beat / 2, .036, True)
            if bar % 2 == 0:
                note(out, start, 2.8 * beat, tones[1] + 12, .046, "horn")
    # The recurring Ember motif changes instrumentation and pulse by game state.
    for bar, entries in melody.items():
        for beat_pos, midi, length in entries:
            voice = ("horn" if kind == "combat" else "flute" if kind == "explore" else "horn")
            note(out, (bar * 4 + beat_pos) * beat, length * beat, midi,
                 .068 if kind == "combat" else .041 if kind == "explore" else .057, voice)
            if kind == "title" and bar >= 8:
                note(out, (bar * 4 + beat_pos) * beat + .04, length * beat, midi - 12, .023, "choir")
    # Wrapped early reflections preserve continuity at the loop boundary.
    dry = out.copy()
    for delay, level in ((.113, .13), (.239, .10), (.397, .075), (.641, .055)):
        out += np.roll(dry, int(RATE * delay)) * level
    # Soft limiting leaves room for the sound effects and other game audio.
    out = np.tanh(out * 1.35)
    peak = max(.01, float(np.max(np.abs(out))))
    pcm = np.int16(np.clip(out / peak * .67, -1, 1) * 32767)
    ASSETS.mkdir(exist_ok=True)
    target = ASSETS / f"music_{name}.wav"
    with wave.open(str(target), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())
    print(f"{target.name}: {len(out)/RATE:.1f}s, peak {np.max(np.abs(pcm))/32767:.3f}")


MOTIF = [
    (0, 62, .9), (1, 65, .75), (2, 69, 1.6),
    (0, 67, .8), (1, 65, .75), (2, 64, .8), (3, 62, 1.3),
]


def phrase(bars, shift=0):
    return {bar: [(pos, midi + shift, dur) for pos, midi, dur in (MOTIF[:3] if bar % 2 == 0 else MOTIF[3:])]
            for bar in bars}


if __name__ == "__main__":
    render("title", 80,
           ["Dm", "Bb", "Gm", "A", "Dm", "C", "Bb", "A", "Gm", "Bb", "Dm", "A", "Gm", "Eb", "A", "Dm"],
           phrase((0, 1, 4, 5, 8, 9, 12, 13)) | {14: [(0, 64, .7), (1, 67, .7), (2, 69, 1.9)], 15: [(0, 74, 2.2)]}, "title")
    render("explore", 72,
           ["Dm", "C", "Bb", "A", "Dm", "Gm", "Eb", "A", "Gm", "Bb", "Dm", "C", "Gm", "Eb", "A", "Dm"],
           phrase((2, 3, 10, 11)), "explore")
    render("combat", 120,
           ["Dm", "Eb", "Bb", "A", "Gm", "Eb", "A", "Dm", "Dm", "C", "Gm", "A", "Bb", "Gm", "A", "Dm", "Eb", "Bb", "Gm", "A", "Dm", "Gm", "A", "Dm"],
           phrase((0, 1, 4, 5, 8, 9, 12, 13, 16, 17, 20, 21)), "combat")
