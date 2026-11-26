# Gera as músicas do jogo (chiptune simples, combinando com a pixel art). Só usa a biblioteca padrão do Python.
# Uso (na raiz do projeto): python tools/audio/music.py
# Saída: sounds/music/tema.wav (intro, menu, final), explorar.wav (andando pelas fases), luta.wav (ondas e chefes),
#        biblioteca.wav (Bibliotecária patrulhando: furtividade), cacada.wav (Bibliotecária te caçando),
#        chefe.wav (Tia da Cantina), fuga.wav (escada desabando), laboratorio.wav (Fase 3), alarme.wav (Fase 3 com
#        o alarme), sia.wav (auditório 3D com a S.I.A.), arcade.wav (Pac-Man dentro do telão) e final.wav (o final).
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
        # emenda do loop: os últimos milissegundos vão chegando no valor do começo (sem "tic" ao repetir)
        seam = int(0.012 * RATE)
        for i in range(seam):
            w = (i + 1) / seam
            j = self.n - seam + i
            self.buf[j] = self.buf[j] * (1 - w) + self.buf[0] * w
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


def tick(vol=0.05):
    """Tique do relógio da biblioteca: estalinho seco de madeira."""
    n = int(0.03 * RATE)
    return [vol * math.sin(2 * math.pi * 2400 * i / RATE) * math.exp(-i / RATE * 180) for i in range(n)]


def music_box(f, dur=1.8, vol=0.07):
    """Caixinha de música velha: um pouco desafinada (dois senos que batem) e com o brilho que some."""
    n = int(dur * RATE)
    return [vol * (math.sin(2 * math.pi * f * i / RATE) + 0.7 * math.sin(2 * math.pi * f * 1.012 * i / RATE)
                   + 0.25 * math.sin(2 * math.pi * f * 3 * i / RATE)) * math.exp(-4 * i / RATE / dur) * env(i, n, 0.002, 0.08)
            for i in range(n)]


def drone(f, dur, vol=0.1):
    """Zumbido grave que respira devagar."""
    n = int(dur * RATE)
    return [vol * (math.sin(2 * math.pi * f * i / RATE) + 0.5 * math.sin(2 * math.pi * f * 1.5 * i / RATE))
            * (0.75 + 0.25 * math.sin(2 * math.pi * 0.12 * i / RATE)) * env(i, n, 1.5, 1.5) for i in range(n)]


def biblioteca():
    """Furtividade na biblioteca: quase silêncio. Relógio, drone grave e uma caixinha de música desafinada.
    Ré frígio (o Mi bemol deixa tudo esquisito), 60 bpm, 16 compassos."""
    rng = random.Random(21)
    bpm = 60
    beat = 60 / bpm
    bar = 4 * beat
    bars = 16
    tr = Track(bars * bar)
    tr.add(0, drone(freq("D2"), bars * bar + 2, 0.09))
    for b in range(bars):
        t0 = b * bar
        # pad grave com a segunda menor (Ré + Mi bemol) entrando e saindo
        if b % 4 in (1, 2):
            tr.add(t0, pad(freq("D#3"), bar + 0.6, 0.03))
        tr.add(t0, pad(freq("A2"), bar + 0.6, 0.025))
        for k in range(4):   # tique-taque do relógio (o "tac" mais fraco)
            tr.add(t0 + k * beat, tick(0.05 if k % 2 == 0 else 0.03))
        # caixinha de música: poucas notas, longe uma da outra
        phrase = ["D5", "D#5", "A4", None, "F5", "D#5", "D5", None,
                  "A5", None, "G#4", "A4", "D5", None, None, "D#5"]
        m = phrase[b]
        if m:
            tr.add(t0 + rng.choice([0, beat, 2 * beat]), music_box(freq(m), 2.4, 0.06))
        if b % 8 == 7:   # um sino distante de vez em quando
            tr.add(t0 + 2 * beat, bell(freq("D#6"), 3.0, 0.035))
    tr.echo(0.53, 0.55, 0.45)
    tr.save("biblioteca.wav", 0.6)


def cacada():
    """A Bibliotecária te viu: coração disparado e um ostinato grave que não resolve.
    Dó sustenido menor com a segunda menor (Ré), 112 bpm, 16 compassos. Sem melodia alegre."""
    rng = random.Random(9)
    bpm = 112
    beat = 60 / bpm
    bar = 4 * beat
    bars = 16
    tr = Track(bars * bar)
    ost = ["C#2", "C#2", "D2", "C#2", "C#2", "G#1", "C#2", "D2"]
    for b in range(bars):
        t0 = b * bar
        tr.add(t0, pad(freq("C#3"), bar + 0.3, 0.035))
        tr.add(t0, pad(freq("D4") if b % 2 else freq("G#3"), bar + 0.3, 0.025))
        for k in range(8):
            tr.add(t0 + k * beat / 2, bass(freq(ost[k]), beat / 2 * 0.7, 0.2))
        # coração: tum-tum, tum-tum
        for k in (0, 2):
            tr.add(t0 + k * beat, kick(0.45))
            tr.add(t0 + k * beat + 0.18, kick(0.3))
        if b % 2 == 1:
            tr.add(t0 + 3.5 * beat, snare(rng, 0.14))
        # cordas nervosas: nota aguda tremendo (tremolo) na segunda metade
        if b >= 8:
            f = freq(["G#4", "A4", "G#4", "C#5", "D5", "C#5", "A4", "G#4"][b - 8])
            n = int(bar * RATE)
            trem = [0.05 * (1 if (i / RATE * f) % 1 < 0.5 else -1) * (0.5 + 0.5 * math.sin(2 * math.pi * 12 * i / RATE)) * env(i, n, 0.3, 0.2)
                    for i in range(n)]
            tr.add(t0, trem)
        for k in range(8):
            if k % 2 == 1:
                tr.add(t0 + k * beat / 2, hat(rng, 0.035))
    tr.echo(0.27, 0.35, 0.3)
    tr.save("cacada.wav", 0.72)


def glide(f0, f1, dur, vol=0.06, duty=0.5):
    """Onda quadrada que escorrega de uma nota para outra (a "sirene")."""
    n = int(dur * RATE)
    out, phase = [], 0.0
    for i in range(n):
        k = i / n
        f = f0 * (f1 / f0) ** (0.5 - 0.5 * math.cos(math.pi * k))
        phase = (phase + f / RATE) % 1.0
        out.append(vol * (1 if phase < duty else -1) * env(i, n, 0.01, 0.05))
    return out


def blip(f, vol=0.05):
    """Bipe de computador."""
    n = int(0.07 * RATE)
    out, phase = [], 0.0
    for i in range(n):
        phase = (phase + f / RATE) % 1.0
        out.append(vol * (1 if phase < 0.5 else -1) * env(i, n, 0.002, 0.02))
    return out


def chefe():
    """Tia da Cantina: chefe meio cômico, meio perigoso. Lá menor, 132 bpm, baixo "tum-pá" de bandinha e
    melodia picadinha em onda quadrada. 16 compassos."""
    rng = random.Random(5)
    bpm = 132
    beat = 60 / bpm
    bar = 4 * beat
    prog = [("A2", "m"), ("A2", "m"), ("D2", "m"), ("E2", "M")] * 4
    tr = Track(len(prog) * bar)
    lead = ["A4", "C5", "E5", "C5", "B4", "G#4", "B4", "E4",
            "A4", "C5", "E5", "A5", "G#5", "E5", "F5", "E5"]
    for b, (root, kind) in enumerate(prog):
        t0 = b * bar
        notes = chord(root[:-1] + "4", kind)
        for k in range(4):   # tum (baixo) - pá (acorde curtinho)
            if k % 2 == 0:
                tr.add(t0 + k * beat, bass(freq(root if k == 0 else root[:-1] + "1" if root[-1] == "2" else root), beat * 0.8, 0.22))
                tr.add(t0 + k * beat, kick(0.4))
            else:
                for nm in notes:
                    tr.add(t0 + k * beat, square(freq(nm), beat * 0.25, 0.025, 0.5))
                tr.add(t0 + k * beat, snare(rng, 0.16))
        for k in range(8):
            tr.add(t0 + k * beat / 2, hat(rng, 0.04))
        # melodia em colcheias, picadinha (staccato)
        m = lead[b % 16]
        for k in range(4):
            nm = m if k % 2 == 0 else notes[k % 3]
            tr.add(t0 + k * beat, square(freq(nm), beat * 0.35, 0.05, 0.25))
    tr.echo(0.23, 0.25, 0.2)
    tr.save("chefe.wav", 0.75)


def fuga():
    """Escada desabando: corre! Ré menor, 160 bpm, baixo em semicolcheias, caixa acelerando no fim de cada frase
    e uma melodia que sobe. 16 compassos."""
    rng = random.Random(8)
    bpm = 160
    beat = 60 / bpm
    bar = 4 * beat
    prog = ["D2", "D2", "A#1", "C2", "D2", "D2", "A#1", "A1"] * 2
    tr = Track(len(prog) * bar)
    climb = ["D5", "E5", "F5", "G5", "A5", "G5", "F5", "E5", "D5", "F5", "A5", "C6", "A#5", "A5", "G5", "A5"]
    for b, root in enumerate(prog):
        t0 = b * bar
        tr.add(t0, pad(freq(root[:-1] + "3"), bar + 0.2, 0.03))
        for k in range(16):
            nm = root if k % 4 != 2 else root[:-1] + str(int(root[-1]) + 1)
            tr.add(t0 + k * beat / 4, bass(freq(nm), beat / 4 * 0.8, 0.17))
        for k in range(4):
            tr.add(t0 + k * beat, kick(0.45) if k % 2 == 0 else snare(rng, 0.2))
        if b % 4 == 3:   # rufo da caixa no fim da frase
            for k in range(8):
                tr.add(t0 + 2 * beat + k * beat / 4, snare(rng, 0.06 + 0.02 * k))
        for k in range(8):
            tr.add(t0 + k * beat / 2, hat(rng, 0.05))
        tr.add(t0, square(freq(climb[b]), beat * 1.8, 0.05, 0.25, 0.01))
        tr.add(t0 + 2 * beat, square(freq(climb[(b + 1) % 16]), beat * 1.8, 0.045, 0.25, 0.01))
    tr.echo(0.19, 0.2, 0.15)
    tr.save("fuga.wav", 0.78)


def laboratorio():
    """Fase 3 (4º andar, laboratórios): ambiente de ficção científica, calmo e frio. Si menor, 80 bpm,
    pulso fininho de máquina, acordes longos e bipes soltos. 16 compassos."""
    rng = random.Random(13)
    bpm = 80
    beat = 60 / bpm
    bar = 4 * beat
    prog = [("B2", "m"), ("G2", "M"), ("D3", "M"), ("A2", "M")] * 4
    tr = Track(len(prog) * bar)
    for b, (root, kind) in enumerate(prog):
        t0 = b * bar
        for nm in chord(root[:-1] + "3", kind):
            tr.add(t0, pad(freq(nm), bar + 0.6, 0.04))
        tr.add(t0, bass(freq(root), bar * 0.6, 0.12))
        for k in range(16):   # pulso de máquina, bem baixinho
            tr.add(t0 + k * beat / 4, square(freq(root[:-1] + "5"), 0.03, 0.012 if k % 4 else 0.02, 0.5))
        for k in range(8):
            if rng.random() < 0.22:
                tr.add(t0 + k * beat / 2, blip(freq(rng.choice(["B5", "D6", "F#6", "A6"])), 0.035))
        if b % 4 == 2:
            tr.add(t0 + beat, bell(freq(rng.choice(["F#5", "B5"])), 2.6, 0.05))
    tr.echo(0.45, 0.5, 0.4)
    tr.save("laboratorio.wav", 0.62)


def alarme():
    """Fase 3 com o alarme: tensa e eletrônica. Mi frígio, 128 bpm, pulso de sintetizador, bateria seca e uma
    "sirene" (nota escorregando) por cima. 16 compassos."""
    rng = random.Random(17)
    bpm = 128
    beat = 60 / bpm
    bar = 4 * beat
    roots = ["E2", "E2", "F2", "E2", "E2", "E2", "G2", "F2"] * 2
    tr = Track(len(roots) * bar)
    for b, root in enumerate(roots):
        t0 = b * bar
        tr.add(t0, pad(freq(root[:-1] + "3"), bar + 0.2, 0.03))
        for k in range(8):   # pulso de sintetizador (oitavas)
            nm = root if k % 2 == 0 else root[:-1] + "3"
            tr.add(t0 + k * beat / 2, square(freq(nm), beat / 2 * 0.6, 0.05, 0.3))
        for k in range(4):
            tr.add(t0 + k * beat, kick(0.45))
            if k % 2 == 1:
                tr.add(t0 + k * beat, snare(rng, 0.18))
        for k in range(8):
            tr.add(t0 + k * beat / 2 + beat / 4, hat(rng, 0.05))
        # sirene: sobe e desce a cada dois compassos
        if b % 2 == 0:
            tr.add(t0, glide(freq("B4"), freq("E5"), bar, 0.035, 0.5))
        else:
            tr.add(t0, glide(freq("E5"), freq("B4"), bar, 0.035, 0.5))
    tr.echo(0.23, 0.3, 0.2)
    tr.save("alarme.wav", 0.72)


def sia():
    """Auditório 3D, a S.I.A. no telão: digital e estranho. Lá menor, 72 bpm, arpejo rápido de computador,
    drone grave e "falhas" (glitches) de vez em quando. 12 compassos."""
    rng = random.Random(23)
    bpm = 72
    beat = 60 / bpm
    bar = 4 * beat
    prog = [("A2", "m"), ("F2", "M"), ("D2", "m"), ("E2", "m")] * 3
    tr = Track(len(prog) * bar)
    tr.add(0, drone(freq("A1"), len(prog) * bar + 2, 0.08))
    for b, (root, kind) in enumerate(prog):
        t0 = b * bar
        notes = chord(root[:-1] + "4", kind)
        for nm in notes:
            tr.add(t0, pad(freq(nm), bar + 0.5, 0.03))
        arp = notes + [notes[0][:-1] + "5"]
        for k in range(16):   # arpejo em semicolcheias, como um processador pensando
            nm = arp[[0, 1, 2, 3, 2, 1][k % 6]]
            tr.add(t0 + k * beat / 4, square(freq(nm), beat / 4 * 0.5, 0.025, 0.125))
        # glitch: bipes em sequência maluca
        if rng.random() < 0.6:
            g0 = t0 + rng.choice([1, 2, 3]) * beat
            for k in range(6):
                tr.add(g0 + k * 0.035, blip(rng.uniform(300, 2400), 0.03))
        if b % 4 == 3:
            tr.add(t0 + beat, bell(freq("E6"), 2.5, 0.04))
    tr.echo(0.4, 0.45, 0.35)
    tr.save("sia.wav", 0.65)


def arcade():
    """Pac-Man dentro do telão: fliperama assombrado. Sol menor, 150 bpm, melodia rápida em onda quadrada,
    baixo saltitante e bateria de videogame. 12 compassos."""
    rng = random.Random(31)
    bpm = 150
    beat = 60 / bpm
    bar = 4 * beat
    prog = ["G2", "G2", "D#2", "F2", "G2", "G2", "C3", "D3"] + ["G2", "D#2", "F2", "D3"]
    tr = Track(len(prog) * bar)
    lead = [["G5", "A#5", "D6", "A#5"], ["C6", "A#5", "A5", "G5"], ["D#5", "G5", "A#5", "G5"], ["F5", "A5", "C6", "A5"],
            ["G5", "D6", "G6", "D6"], ["F6", "D#6", "D6", "A#5"], ["C6", "D#6", "C6", "G5"], ["D6", "C6", "A5", "F#5"],
            ["G5", "A#5", "D6", "G6"], ["D#6", "D6", "A#5", "G5"], ["F5", "A5", "C6", "D6"], ["D6", "A5", "F#5", "D5"]]
    for b, root in enumerate(prog):
        t0 = b * bar
        for k in range(8):   # baixo saltitante (fundamental e oitava)
            nm = root if k % 2 == 0 else root[:-1] + str(int(root[-1]) + 1)
            tr.add(t0 + k * beat / 2, square(freq(nm), beat / 2 * 0.7, 0.05, 0.5))
        for k in range(4):
            tr.add(t0 + k * beat, kick(0.3) if k % 2 == 0 else snare(rng, 0.12))
        for k, nm in enumerate(lead[b]):
            tr.add(t0 + k * beat, square(freq(nm), beat * 0.8, 0.05, 0.25))
    tr.echo(0.2, 0.2, 0.12)
    tr.save("arcade.wav", 0.72)


def final():
    """O final (diploma na mão): emocionante e esperançoso. Dó maior, 92 bpm, acordes cheios, arpejo e um sino
    tocando a melodia; na segunda metade entra uma bateria leve. 16 compassos."""
    rng = random.Random(41)
    bpm = 92
    beat = 60 / bpm
    bar = 4 * beat
    prog = [("C3", "M"), ("G2", "M"), ("A2", "m"), ("F2", "M")] * 4
    tr = Track(len(prog) * bar)
    melody = ["E5", "D5", "C5", "C5", "E5", "G5", "A5", "F5",
              "G5", "D5", "E5", "C5", "E5", "G5", "C6", "C6"]
    for b, (root, kind) in enumerate(prog):
        t0 = b * bar
        notes = chord(root[:-1] + "4", kind)
        for nm in notes:
            tr.add(t0, pad(freq(nm), bar + 0.4, 0.05))
        tr.add(t0, bass(freq(root), bar * 0.45, 0.16))
        tr.add(t0 + 2 * beat, bass(freq(root), bar * 0.45, 0.12))
        arp = notes + [notes[0][:-1] + "5"]
        for k in range(8):
            tr.add(t0 + k * beat / 2, pluck(freq(arp[[0, 1, 2, 3, 2, 1, 2, 3][k]]), 0.6, 0.07))
        tr.add(t0, bell(freq(melody[b]), bar * 0.6, 0.1))
        tr.add(t0 + 2.5 * beat, bell(freq(melody[(b + 1) % 16]), bar * 0.35, 0.06))
        if b >= 8:
            for k in range(4):
                tr.add(t0 + k * beat, kick(0.25) if k % 2 == 0 else snare(rng, 0.08))
    tr.echo(0.33, 0.4, 0.3)
    tr.save("final.wav", 0.72)


if __name__ == "__main__":
    tema()
    explorar()
    luta()
    biblioteca()
    cacada()
    chefe()
    fuga()
    laboratorio()
    alarme()
    sia()
    arcade()
    final()
