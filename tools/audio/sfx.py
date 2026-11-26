# Efeitos sonoros gerados (só biblioteca padrão do Python).
# Uso (na raiz do projeto): python tools/audio/sfx.py
# Saída:
#   sounds/player/tiro_silencio.wav   tiro do Rifle do Silêncio: "pfft" abafado + um "shhh" que some
#   sounds/objects/campainha.wav      campainha de balcão (ding!)
#   sounds/objects/radio.wav          rádio velho chiando e tocando um pedacinho de música
#   sounds/objects/despertador.wav    despertador tocando (trrrim)
#   sounds/player/tiro_grampeador.wav  tiro do Grampeador (ka-tchak)
#   sounds/player/tiro_escopeta_cafe.wav  tiro da Escopeta de Café (pof + vapor)
#   sounds/objects/telefone.wav       telefone antigo tocando
#   sounds/objects/livros.wav         pilha de livros caindo
import math, os, random, struct, wave

RATE = 22050
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def save(rel, samples, volume=0.8):
    peak = max(1e-6, max(abs(v) for v in samples))
    k = volume / peak
    path = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, v * k)) * 32767)) for v in samples))
    print("ok", rel)


def lowpass(samples, a):
    out, y = [], 0.0
    for x in samples:
        y += a * (x - y)
        out.append(y)
    return out


def tiro_silencio():
    rng = random.Random(1)
    n = int(0.55 * RATE)
    thump, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        phase += (55 + 140 * math.exp(-t * 40)) / RATE
        thump.append(math.sin(2 * math.pi * phase) * math.exp(-t * 28))
    pfft = lowpass([rng.uniform(-1, 1) * math.exp(-i / RATE * 30) for i in range(n)], 0.15)
    # "shhh": chiado agudo que entra logo depois e some devagar
    shh_raw = [rng.uniform(-1, 1) for _ in range(n)]
    shh_lp = lowpass(shh_raw, 0.5)
    shh = [(shh_raw[i] - shh_lp[i]) * min(1, max(0, (i / RATE - 0.04) * 20)) * math.exp(-max(0, i / RATE - 0.06) * 7) for i in range(n)]
    return [0.9 * thump[i] + 0.8 * pfft[i] + 0.35 * shh[i] for i in range(n)]


def campainha():
    n = int(1.4 * RATE)
    out = []
    for i in range(n):
        t = i / RATE
        v = math.sin(2 * math.pi * 1760 * t) + 0.5 * math.sin(2 * math.pi * 1760 * 2.4 * t) + 0.25 * math.sin(2 * math.pi * 1760 * 4.1 * t)
        out.append(v * math.exp(-t * 3.5) * min(1, t * 400))
    return out


def radio():
    rng = random.Random(4)
    n = int(3.0 * RATE)
    raw = [rng.uniform(-1, 1) for _ in range(n)]
    hiss = lowpass(raw, 0.35)
    melody = [440, 523, 587, 523, 440, 392, 440, 0]
    out = []
    for i in range(n):
        t = i / RATE
        f = melody[int(t * 4) % len(melody)]
        tone = 0.0 if f == 0 else (1 if (t * f) % 1 < 0.5 else -1) * 0.35   # musiquinha de rádio AM
        fade = 0.5 + 0.5 * math.sin(2 * math.pi * 0.7 * t)                    # sintonia indo e voltando
        out.append((hiss[i] * (1 - 0.6 * fade) + tone * fade) * min(1, t * 20) * min(1, (3.0 - t) * 6))
    return lowpass(out, 0.6)


def despertador():
    n = int(2.6 * RATE)
    out = []
    for i in range(n):
        t = i / RATE
        on = (t % 0.5) < 0.38                       # trrrim... trrrim...
        hammer = 1 if (t * 28) % 1 < 0.5 else 0.3   # o martelinho batendo nos sinos
        v = math.sin(2 * math.pi * 2100 * t) + 0.6 * math.sin(2 * math.pi * 2650 * t)
        out.append(v * hammer * (1 if on else 0) * min(1, (2.6 - t) * 8))
    return lowpass(out, 0.7)


def grampeador():
    """Grampeador: "ka-tchak" metálico (dois estalos e um rangido curto de mola)."""
    rng = random.Random(2)
    n = int(0.22 * RATE)
    out = []
    for i in range(n):
        t = i / RATE
        v = 0.0
        for t0, f, decay in ((0.0, 3100, 90), (0.05, 2300, 70)):
            if t >= t0:
                tt = t - t0
                v += (math.sin(2 * math.pi * f * tt) * 0.6 + rng.uniform(-1, 1) * 0.5) * math.exp(-tt * decay)
        if 0.05 <= t < 0.15:   # mola
            v += 0.15 * math.sin(2 * math.pi * (900 + 1500 * (t - 0.05)) * t) * math.exp(-(t - 0.05) * 25)
        out.append(v)
    return out


def escopeta_cafe():
    """Escopeta de Café: "POF" grave (a tampa da garrafa térmica) e um chiado de vapor que some."""
    rng = random.Random(6)
    n = int(0.7 * RATE)
    boom, phase = [], 0.0
    for i in range(n):
        t = i / RATE
        phase += (45 + 110 * math.exp(-t * 25)) / RATE
        boom.append(math.sin(2 * math.pi * phase) * math.exp(-t * 14))
    crack = lowpass([rng.uniform(-1, 1) * math.exp(-i / RATE * 40) for i in range(n)], 0.35)
    raw = [rng.uniform(-1, 1) for _ in range(n)]
    low = lowpass(raw, 0.6)
    steam = [(raw[i] - low[i]) * min(1, max(0, (i / RATE - 0.05) * 12)) * math.exp(-max(0, i / RATE - 0.08) * 5) for i in range(n)]
    return [boom[i] + 0.9 * crack[i] + 0.3 * steam[i] for i in range(n)]


def telefone():
    """Telefone antigo de disco: "trrrim... trrrim" (campainha dupla batendo rápido)."""
    n = int(2.8 * RATE)
    out = []
    for i in range(n):
        t = i / RATE
        on = (t % 1.4) < 0.9
        hammer = 1 if (t * 22) % 1 < 0.5 else 0.25
        v = math.sin(2 * math.pi * 1400 * t) + 0.7 * math.sin(2 * math.pi * 1750 * t) + 0.2 * math.sin(2 * math.pi * 3500 * t)
        out.append(v * hammer * (1 if on else 0) * min(1, (2.8 - t) * 8))
    return lowpass(out, 0.75)


def livros():
    """Pilha de livros caindo: vários "tum" de livro batendo no chão, um atrás do outro."""
    rng = random.Random(12)
    n = int(1.3 * RATE)
    out = [0.0] * n
    t0 = 0.0
    for k in range(7):
        t0 += rng.uniform(0.05, 0.16)
        f = rng.uniform(70, 130)
        for i in range(int(0.25 * RATE)):
            j = int(t0 * RATE) + i
            if j >= n: break
            tt = i / RATE
            out[j] += (math.sin(2 * math.pi * f * tt) + rng.uniform(-1, 1) * 0.6 * math.exp(-tt * 60)) * math.exp(-tt * 18) * (1 - k * 0.08)
    return lowpass(out, 0.5)


if __name__ == "__main__":
    save("sounds/player/tiro_grampeador.wav", grampeador(), 0.7)
    save("sounds/player/tiro_escopeta_cafe.wav", escopeta_cafe(), 0.85)
    save("sounds/objects/telefone.wav", telefone(), 0.55)
    save("sounds/objects/livros.wav", livros(), 0.8)
    save("sounds/player/tiro_silencio.wav", tiro_silencio(), 0.8)
    save("sounds/objects/campainha.wav", campainha(), 0.7)
    save("sounds/objects/radio.wav", radio(), 0.6)
    save("sounds/objects/despertador.wav", despertador(), 0.55)
