# Música da abertura (letreiro subindo no espaço): fanfarra de metais "ópera espacial", composição original.
# Tudo sintetizado com numpy (sem gravação de terceiros).
#   python tools/audio/gen_intro.py   ->   sounds/intro/abertura.ogg (~30 s, toca uma vez)
# Estrutura (108 bpm, Si bemol maior):
#   compassos 1-8   fanfarra dos metais com tímpanos e cordas
#   compassos 9-12  trecho sombrio em sol menor (os fantasmas), trompa grave e cordas
#   compassos 13-14 volta a fanfarra e acorde final longo
import os
import subprocess
import numpy as np
from scipy.io import wavfile
from scipy.signal import lfilter, butter

SR = 44100
BPM = 108
BEAT = 60 / BPM
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "sounds", "intro")
rng = np.random.default_rng(1977)

NOTES = {"C": 0, "Db": 1, "D": 2, "Eb": 3, "E": 4, "F": 5, "Gb": 6, "G": 7, "Ab": 8, "A": 9, "Bb": 10, "B": 11}


def hz(name):
    """'Bb4' -> frequência."""
    pitch, octave = name[:-1], int(name[-1])
    midi = 12 * (octave + 1) + NOTES[pitch]
    return 440.0 * 2 ** ((midi - 69) / 12)


def lowpass(x, cutoff):
    b, a = butter(2, cutoff / (SR / 2))
    return lfilter(b, a, x)


def adsr(n, a, d, s, r):
    e = np.full(n, s)
    a, d, r = int(SR * a), int(SR * d), int(SR * r)
    a = min(a, n); e[:a] = np.linspace(0, 1, a)
    d = min(d, n - a); e[a:a + d] = np.linspace(1, s, d)
    r = min(r, n); e[n - r:] *= np.linspace(1, 0, r)
    return e


def brass(freq, dur, vel=1.0):
    """Metal: harmônicos com brilho que abre no ataque, um pouco de vibrato."""
    n = int(SR * dur)
    x = np.arange(n) / SR
    vib = 1 + 0.004 * np.sin(2 * np.pi * 5.2 * x) * np.clip(x / 0.4, 0, 1)
    phase = 2 * np.pi * np.cumsum(freq * vib) / SR
    bright = 0.45 + 0.55 * np.exp(-x / 0.25)
    out = np.zeros(n)
    for k in range(1, 12):
        out += np.sin(k * phase) * (bright ** (k - 1)) / k ** 0.9
    return out * adsr(n, 0.03, 0.12, 0.8, min(0.12, dur * 0.4)) * vel


def strings(freq, dur, vel=1.0):
    """Cordas: serras levemente desafinadas, filtradas, ataque lento."""
    n = int(SR * dur)
    x = np.arange(n) / SR
    out = np.zeros(n)
    for detune in (-0.004, 0.0, 0.0045):
        ph = (freq * (1 + detune) * x + rng.random()) % 1.0
        out += 2 * ph - 1
    out = lowpass(out, min(3200, freq * 6))
    return out * adsr(n, 0.25, 0.2, 0.85, min(0.5, dur * 0.5)) * vel / 3


def timpani(freq, dur=1.6, vel=1.0):
    n = int(SR * dur)
    x = np.arange(n) / SR
    f = freq * (1 + 0.08 * np.exp(-x / 0.05))
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-x / 0.55)
    body += 0.4 * np.sin(2 * np.pi * np.cumsum(f * 1.5) / SR) * np.exp(-x / 0.3)
    hit = lowpass(rng.standard_normal(n), 900) * np.exp(-x / 0.03) * 0.6
    return (body + hit) * vel


def cymbal(dur=3.0, vel=0.25):
    n = int(SR * dur)
    x = np.arange(n) / SR
    noise = rng.standard_normal(n)
    noise = noise - lowpass(noise, 5000)
    return noise * np.exp(-x / 1.1) * vel


class Track:
    def __init__(self, seconds):
        self.left = np.zeros(int(SR * seconds))
        self.right = np.zeros(int(SR * seconds))

    def add(self, sound, beat, pan=0.0):
        start = int(SR * beat * BEAT)
        end = min(start + len(sound), len(self.left))
        sound = sound[: end - start]
        self.left[start:end] += sound * (1 - max(pan, 0))
        self.right[start:end] += sound * (1 + min(pan, 0))


def play_line(track, line, start_beat, voice, pan=0.0, vel=1.0, octave_double=False):
    """line: lista de (nota ou None, duração em tempos)."""
    b = start_beat
    for note, beats in line:
        if note:
            dur = beats * BEAT * 0.97
            track.add(voice(hz(note), dur, vel), b, pan)
            if octave_double:
                low = note[:-1] + str(int(note[-1]) - 1)
                track.add(voice(hz(low), dur, vel * 0.5), b, -pan)
        b += beats
    return b


def chords(track, progression, start_beat, beats_each, vel=0.5):
    b = start_beat
    for chord in progression:
        for i, note in enumerate(chord):
            track.add(strings(hz(note), beats_each * BEAT * 1.02, vel), b, (-0.5, 0.0, 0.5, 0.2)[i % 4])
        b += beats_each


def reverb(x, seconds=2.2, mix=0.28):
    n = int(SR * seconds)
    tail = rng.standard_normal(n) * np.exp(-np.arange(n) / SR / (seconds / 5))
    tail = lowpass(tail, 4500)
    tail /= np.sqrt((tail ** 2).sum())
    wet = np.convolve(x, tail)[: len(x)]
    return x * (1 - mix) + wet * mix * 3


def main():
    total_beats = 56
    tr = Track(total_beats * BEAT + 3.0)
    T3 = 1 / 3

    # ---------- compassos 1-8: fanfarra (Si bemol maior) ----------
    tr.add(timpani(hz("Bb1"), 2.0, 1.2), 0)
    tr.add(cymbal(3.5, 0.3), 0)
    tr.add(timpani(hz("F2"), 1.2, 0.7), 3.5)
    fanfare_a = [("F4", T3), ("F4", T3), ("F4", T3), ("Bb4", 1), ("C5", 0.5), ("D5", 0.5), ("Eb5", 2), (None, 0),
                 ("D5", T3), ("C5", T3), ("Bb4", T3), ("G4", 3), (None, 1)]
    fanfare_b = [("Bb4", T3), ("C5", T3), ("D5", T3), ("F5", 2), ("Eb5", 0.5), ("D5", 0.5), ("C5", 1.5), ("Bb4", 0.5),
                 ("D5", 3), (None, 1)]
    b = play_line(tr, fanfare_a, 0, brass, pan=0.15, vel=0.9, octave_double=True)
    b = play_line(tr, fanfare_b, b, brass, pan=0.15, vel=0.95, octave_double=True)
    answer = [("D5", 2), ("C5", 1), ("Bb4", 1), ("C5", 3), ("F4", 1), ("G4", 1.5), ("A4", 0.5), ("Bb4", 1), ("A4", 1)]
    play_line(tr, answer, b, brass, pan=-0.2, vel=0.7)
    chords(tr, [("Bb2", "F3", "D4"), ("Eb3", "G3", "Bb3"), ("G2", "D3", "Bb3"), ("F2", "C3", "A3"),
                ("Bb2", "F3", "D4"), ("G2", "Bb3", "D4"), ("Eb3", "G3", "C4"), ("F2", "A3", "C4")], 0, 4, 0.45)
    for beat in (8, 16, 24):
        tr.add(timpani(hz("Bb1") if beat != 16 else hz("F2"), 1.4, 0.75), beat)
    tr.add(cymbal(3.0, 0.22), 16)

    # ---------- compassos 9-12: sombrio (sol menor), trompa grave ----------
    dark = [("G3", 3), ("Bb3", 1), ("A3", 2), ("D3", 2), ("Eb3", 3), ("D3", 1), ("D3", 4)]
    play_line(tr, dark, 32, brass, pan=-0.3, vel=0.75, octave_double=True)
    chords(tr, [("G2", "D3", "Bb3"), ("D2", "A2", "Gb3"), ("Eb2", "Bb2", "G3"), ("D2", "A2", "D3")],
           32, 4, 0.4)
    for beat in range(32, 48, 2):   # tímpano marcando como passos no escuro
        tr.add(timpani(hz("G1") if beat % 4 == 0 else hz("D2"), 0.9, 0.5), beat)

    # ---------- compassos 13-14: volta a fanfarra e acorde final ----------
    tr.add(timpani(hz("F2"), 0.6, 0.6), 47); tr.add(timpani(hz("F2"), 0.6, 0.7), 47.5)
    final = [("F4", T3), ("F4", T3), ("F4", T3), ("Bb4", 1), ("D5", 1), ("F5", 5)]
    play_line(tr, final, 48, brass, pan=0.15, vel=1.0, octave_double=True)
    chords(tr, [("Bb2", "F3", "D4", "F4")], 49, 7, 0.55)
    tr.add(timpani(hz("Bb1"), 3.0, 1.1), 49)
    tr.add(cymbal(5.0, 0.3), 49)

    left, right = reverb(tr.left), reverb(tr.right)
    peak = max(np.abs(left).max(), np.abs(right).max())
    stereo = np.stack([left, right], axis=1) / peak * 0.85
    fade = int(SR * 1.5)
    stereo[-fade:] *= np.linspace(1, 0, fade)[:, None]

    os.makedirs(OUT, exist_ok=True)
    wav = os.path.join(OUT, "abertura.wav")
    ogg = os.path.join(OUT, "abertura.ogg")
    wavfile.write(wav, SR, (stereo * 32767).astype(np.int16))
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "5", ogg], check=True)
    os.remove(wav)
    print("abertura ok:", round(len(stereo) / SR, 1), "s")


if __name__ == "__main__":
    main()
