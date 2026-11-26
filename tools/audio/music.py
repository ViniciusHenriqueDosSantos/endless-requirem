# Gera as músicas do jogo (chiptune simples, combinando com a pixel art). Só usa a biblioteca padrão do Python.
# Uso (na raiz do projeto): python tools/audio/music.py
# Saída: sounds/music/tema.wav (intro, menu, final), explorar.wav (andando pelas fases) e luta.wav (ondas e chefes).
# Cada música é um loop: o fim emenda no começo (o rabo do eco volta para o início).
import math, os, random, struct, wave

RATE = 22050
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "sounds", "music")

NOTES = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5, "F#": 6, "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}


def freq(name):
    """'A4' -> 440 Hz."""
    note, octave = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((NOTES[note] + 12 * (octave - 4) - 9) / 12)


class Track:
    def __init__(self, seconds):
        self.n = int(seconds * RATE)
        self.buf = [0.0] * self.n

    def add(self, start, samples, gain=1.0):
        i0 = int(start * RATE)
        for i, v in enumerate(samples):
            j = (i0 + i) % self.n          # o que passa do fim volta para o começo (loop sem emenda)
            self.buf[j] += v * gain

    def echo(self, delay, feedback, mix):
        d = int(delay * RATE)
        out = self.buf[:]
        for _ in range(2):                  # duas voltas: o eco do fim continua no começo
            for i in range(self.n):
                out[i] = self.buf[i] + out[(i - d) % self.n] * feedback
        self.buf = [self.buf[i] * (1 - mix) + out[i] * mix for i in range(self.n)]

    def save(self, name, volume=0.8):
        peak = max(1e-6, max(abs(v) for v in self.buf))
        k = volume / peak
        os.makedirs(OUT, exist_ok=True)
        with wave.open(os.path.join(OUT, name), "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(RATE)
            w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, v * k)) * 32767)) for v in self.buf))
        print("ok", name, f"{self.n / RATE:.1f} s")


# ---------- instrumentos ----------

def env(i, n, attack, release):
    a = min(1.0, i / max(1, attack * RATE))
    r = min(1.0, (n - i) / max(1, release * RATE))
    return a * r


def pad(f, dur, vol=0.12):
    """Acorde de fundo: soma de harmônicos (som de órgão abafado), entra e sai devagar, levemente desafinado."""
    n = int(dur * RATE)
    out = []
    for i in range(n):
        t = i / RATE
        v = 0.0
        for h, amp in ((1, 1.0), (2, 0.35), (3, 0.15)):
            v += amp * math.sin(2 * math.pi * f * h * t) + amp * 0.6 * math.sin(2 * math.pi * f * h * 1.004 * t)
        out.append(v * vol * env(i, n, 0.6, 0.8))
    return out


def pluck(f, dur=0.6, vol=0.25, bright=0.5):
    """Nota de arpejo: seno + oitava, cai rápido."""
    n = int(dur * RATE)
    return [vol * math.exp(-5 * i / RATE / dur * 1.5) *
            (math.sin(2 * math.pi * f * i / RATE) + bright * math.sin(4 * math.pi * f * i / RATE)) * env(i, n, 0.003, 0.05)
            for i in range(n)]


def bell(f, dur=1.6, vol=0.18):
    """Sino fantasma: harmônicos fora do lugar e um tremor."""
    n = int(dur * RATE)
    out = []
    for i in range(n):
        t = i / RATE
        trem = 1 + 0.15 * math.sin(2 * math.pi * 5 * t)
        v = math.sin(2 * math.pi * f * t) + 0.4 * math.sin(2 * math.pi * f * 2.76 * t) + 0.2 * math.sin(2 * math.pi * f * 5.4 * t)
        out.append(vol * v * trem * math.exp(-3 * t / dur) * env(i, n, 0.005, 0.1))
    return out


def square(f, dur, vol=0.08, duty=0.25, vibrato=0.0):
    """Voz de videogame (onda quadrada fininha)."""
    n = int(dur * RATE)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        ff = f * (1 + vibrato * math.sin(2 * math.pi * 6 * t) * min(1, t * 3))
        phase = (phase + ff / RATE) % 1.0
        out.append(vol * (1 if phase < duty else -1) * env(i, n, 0.005, 0.04))
    return out


def bass(f, dur, vol=0.22):
    """Baixo triangular com um pouco de ataque."""
    n = int(dur * RATE)
    out, phase = [], 0.0
    for i in range(n):
        phase = (phase + f / RATE) % 1.0
        tri = 4 * abs(phase - 0.5) - 1
        out.append(vol * tri * (0.6 + 0.4 * math.exp(-8 * i / RATE)) * env(i, n, 0.004, 0.03))
    return out


def kick(vol=0.55):
    n = int(0.22 * RATE)
    out, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        f = 40 + 90 * math.exp(-t * 30)
        phase += f / RATE
        out.append(vol * math.sin(2 * math.pi * phase) * math.exp(-t * 14))
    return out


def snare(rng, vol=0.25):
    n = int(0.16 * RATE)
    return [vol * (rng.uniform(-1, 1) * 0.8 + 0.3 * math.sin(2 * math.pi * 190 * i / RATE)) * math.exp(-i / RATE * 22) for i in range(n)]


def hat(rng, vol=0.06):
    n = int(0.05 * RATE)
    out, last = [], 0.0
    for i in range(n):
        x = rng.uniform(-1, 1)
        out.append(vol * (x - last) * math.exp(-i / RATE * 70))   # diferença = só os agudos
        last = x
    return out


def chord(root, kind):
    """Notas de um acorde (tríade) a partir da fundamental: 'm' menor, 'M' maior."""
    third = 3 if kind == "m" else 4
    base = NOTES[root[:-1]] + 12 * int(root[-1])
    names = []
    for semis in (0, third, 7):
        v = base + semis
        names.append([k for k, s in NOTES.items() if s == v % 12][0] + str(v // 12))
    return names


# ---------- músicas ----------

def tema():
    """Intro, menu e final: lento e melancólico (o campus abandonado). Lá menor, 80 bpm, 16 compassos."""
    bpm = 80
    beat = 60 / bpm
    bar = 4 * beat
    prog = [("A3", "m"), ("F3", "M"), ("C3", "M"), ("E3", "M")] * 4
    tr = Track(len(prog) * bar)
    melody = [None, "E5", "C5", "B4", "A4", None, "G4", "A4",   # entra na segunda metade
              "C5", "B4", "A4", "E4", "F4", "E4", "G#4", "A4"]
    for b, (root, kind) in enumerate(prog):
        notes = chord(root, kind)
        t0 = b * bar
        for nm in notes:
            tr.add(t0, pad(freq(nm), bar + 0.4, 0.07))
        tr.add(t0, bass(freq(root[:-1] + "2"), bar * 0.9, 0.16))
        arp = notes + [notes[1][:-1] + str(int(notes[1][-1]) + 1)]
        for k in range(8):   # arpejo em colcheias
            nm = arp[[0, 1, 2, 3, 2, 1, 2, 1][k]]
            nm = nm[:-1] + str(int(nm[-1]) + 1)
            tr.add(t0 + k * beat / 2, pluck(freq(nm), 0.7, 0.10))
        if b >= 8 and melody[b - 8 if b - 8 < len(melody) else 0]:
            m = melody[b - 8]
            if m:
                tr.add(t0, bell(freq(m), bar * 0.9, 0.12))
                tr.add(t0 + 2 * beat, bell(freq(m), bar * 0.45, 0.06))
    tr.echo(0.37, 0.45, 0.35)
    tr.save("tema.wav", 0.7)


def explorar():
    """Andando pelas fases: escuro e espaçado, para não cansar. Ré menor, 70 bpm, 16 compassos."""
    rng = random.Random(7)
    bpm = 70
    beat = 60 / bpm
    bar = 4 * beat
    bars = 16
    tr = Track(bars * bar)
    roots = ["D2", "D2", "A#1", "A#1", "G1", "G1", "A1", "A1"] * 2
    scale = ["D4", "F4", "G4", "A4", "C5", "D5", "F5"]
    for b in range(bars):
        t0 = b * bar
        r = roots[b]
        tr.add(t0, pad(freq(r[:-1] + "3"), bar + 0.5, 0.05))
        tr.add(t0, pad(freq("A3" if r[:-1] in ("D", "A") else "D4"), bar + 0.5, 0.035))
        tr.add(t0, bass(freq(r), beat * 1.5, 0.2))
        tr.add(t0 + 2.5 * beat, bass(freq(r), beat * 0.8, 0.12))
        # notas soltas, como gotas no escuro
        for k in range(8):
            if rng.random() < 0.38:
                tr.add(t0 + k * beat / 2, pluck(freq(rng.choice(scale)), 1.1, 0.07, 0.2))
        if b % 4 == 3:
            tr.add(t0 + beat, bell(freq(rng.choice(["A5", "D6", "F5"])), 2.4, 0.06))
        for k in range(4):   # batida de coração bem baixinha
            if k in (0, 2):
                tr.add(t0 + k * beat, kick(0.12))
    tr.echo(0.43, 0.5, 0.4)
    tr.save("explorar.wav", 0.65)


def luta():
    """Ondas de fantasmas e chefes: rápida, com bateria. Mi menor, 140 bpm, 16 compassos."""
    rng = random.Random(3)
    bpm = 140
    beat = 60 / bpm
    bar = 4 * beat
    prog = [("E2", "m"), ("E2", "m"), ("C2", "M"), ("D2", "M"), ("E2", "m"), ("E2", "m"), ("C2", "M"), ("B1", "M")] * 2
    tr = Track(len(prog) * bar)
    lead = ["E5", "G5", "B5", "A5", "G5", "F#5", "E5", "D5",
            "E5", "G5", "B5", "D6", "C6", "B5", "A5", "B5"]
    for b, (root, kind) in enumerate(prog):
        t0 = b * bar
        notes = chord(root[:-1] + "4", kind)
        for nm in notes:
            tr.add(t0, pad(freq(nm), bar + 0.2, 0.035))
        # baixo em colcheias com oitava
        for k in range(8):
            nm = root if k % 2 == 0 else root[:-1] + str(int(root[-1]) + 1)
            tr.add(t0 + k * beat / 2, bass(freq(nm), beat / 2 * 0.9, 0.2))
        # bateria
        for k in range(4):
            if k in (0, 2):
                tr.add(t0 + k * beat, kick())
            else:
                tr.add(t0 + k * beat, snare(rng))
        if b % 2 == 1:
            tr.add(t0 + 3.5 * beat, kick(0.4))
        for k in range(8):
            tr.add(t0 + k * beat / 2, hat(rng, 0.07 if k % 2 else 0.045))
        # melodia na segunda metade (a primeira fica só no ritmo)
        if b >= 8:
            for k in range(2):
                tr.add(t0 + k * 2 * beat, square(freq(lead[(b - 8) * 2 + k]), beat * 1.8, 0.06, 0.25, 0.01))
        else:
            for k in range(4):   # resposta curta no grave enquanto a melodia não entra
                if k % 2 == 1:
                    tr.add(t0 + k * beat + beat / 2, square(freq(notes[k % 3]), beat * 0.4, 0.04, 0.5))
    tr.echo(0.21, 0.25, 0.18)
    tr.save("luta.wav", 0.75)


if __name__ == "__main__":
    tema()
    explorar()
    luta()
