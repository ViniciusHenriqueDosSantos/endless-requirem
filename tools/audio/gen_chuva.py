# Gera os sons da chuva da Fase 1 (sounds/clima/): o loop da chuva e três trovões.
# Tudo sintetizado (ruído filtrado + gotas), sem gravação de terceiros. Precisa de numpy, scipy e ffmpeg no PATH.
#   python tools/audio/gen_chuva.py
import os, subprocess, tempfile
import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, sosfilt

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "sounds", "clima")
rng = np.random.default_rng(2061)


def filt(x, kind, f, order=2):
    return sosfilt(butter(order, f, btype=kind, fs=SR, output="sos"), x)


def brown(n):
    x = np.cumsum(rng.standard_normal(n))
    x -= filt(x, "lowpass", 8)  # tira a deriva
    return x / (np.abs(x).max() + 1e-9)


def drops(n, rate, f_lo, f_hi, dur_lo, dur_hi, amp_lo, amp_hi):
    """Gotas: estalos curtos de ruído filtrado com queda exponencial."""
    out = np.zeros(n)
    k = rng.poisson(rate * n / SR)
    for _ in range(k):
        d = int(SR * rng.uniform(dur_lo, dur_hi))
        start = rng.integers(0, n - d)
        t = np.arange(d) / SR
        f = rng.uniform(f_lo, f_hi)
        burst = rng.standard_normal(d) * 0.6 + np.sin(2 * np.pi * f * t * (1 - 0.3 * t / t[-1]))
        out[start:start + d] += burst * np.exp(-t / (d / SR / 5)) * rng.uniform(amp_lo, amp_hi)
    return out


def normalize(x, rms_db, ceil_db=-1):
    """Ajusta pelo volume médio (RMS) e segura os picos com um limitador suave."""
    x = x / (np.sqrt(np.mean(x ** 2)) + 1e-9) * 10 ** (rms_db / 20)
    c = 10 ** (ceil_db / 20)
    return np.tanh(x / c) * c


def save(name, stereo):
    os.makedirs(OUT, exist_ok=True)
    with tempfile.TemporaryDirectory() as d:
        wav = os.path.join(d, "a.wav")
        wavfile.write(wav, SR, (np.clip(stereo, -1, 1) * 32767).astype(np.int16))
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "5",
                        os.path.join(OUT, name)], check=True)
    print("ok", name)


# ---------- chuva em loop (12 s, o fim se funde com o começo)
def rain_channel(n):
    hiss = filt(filt(rng.standard_normal(n), "highpass", 500), "lowpass", 7000) * 0.55
    body = filt(rng.standard_normal(n), "bandpass", [180, 900]) * 0.35      # o "chiado grosso" de chuva forte
    low = filt(brown(n), "lowpass", 160) * 0.5                               # ronco da água no chão
    fine = filt(drops(n, 900, 2500, 6000, 0.002, 0.006, 0.05, 0.25), "highpass", 1200)
    big = drops(n, 7, 700, 1600, 0.02, 0.05, 0.3, 0.8)                        # pingos de calha, mais graves
    return hiss + body + low + fine * 0.6 + filt(big, "bandpass", [400, 3000]) * 0.5


LOOP, FADE = 12.0, 1.5
n, nf = int(SR * LOOP), int(SR * FADE)
chans = []
for c in range(2):
    x = rain_channel(n + nf)
    t = np.arange(n + nf) / SR
    x *= 0.85 + 0.15 * np.sin(2 * np.pi * t / 6 + c)  # rajadas leves (período divide o loop)
    head = x[:nf].copy()
    w = np.linspace(0, 1, nf)
    x[:nf] = head * w + x[n:n + nf] * (1 - w)  # crossfade: o loop não tem emenda
    chans.append(x[:n])
rain = np.stack(chans, 1)
save("chuva.ogg", normalize(rain, -18))


# ---------- trovões
def thunder(length, crack, rumble_hz, bumps, seed):
    global rng
    rng = np.random.default_rng(seed)
    n = int(SR * length)
    t = np.arange(n) / SR
    env = 1.2 * np.exp(-((t - 0.35) / 0.5) ** 2)  # a primeira onda vem logo depois do clarão
    for _ in range(bumps):  # o ronco vem em ondas que vão sumindo
        c = rng.uniform(0.05, length * 0.6)
        w = rng.uniform(0.25, 0.9)
        env += rng.uniform(0.4, 1) * np.exp(-((t - c) / w) ** 2) * np.exp(-c / (length * 0.45))
    env *= np.minimum(1, t / 0.04)
    env *= np.exp(-t / (length * 0.5))
    out = []
    for c in range(2):
        r = filt(brown(n), "lowpass", rumble_hz) + 0.25 * filt(rng.standard_normal(n), "bandpass", [60, 400])
        x = r * env
        if crack:  # estalo do raio perto: rajadas agudas no começo
            ce = np.zeros(n)
            for _ in range(5):
                s = int(SR * rng.uniform(0, 0.25))
                d = int(SR * rng.uniform(0.04, 0.12))
                ce[s:s + d] += np.exp(-np.arange(d) / (d / 4)) * rng.uniform(0.5, 1)
            x += filt(rng.standard_normal(n), "highpass", 900) * ce * crack
            x += filt(rng.standard_normal(n), "bandpass", [150, 1200]) * ce * crack * 0.8
        out.append(x)
    x = np.stack(out, 1)
    fade = np.minimum(1, (n - np.arange(n)) / (SR * 0.6))[:, None]
    return x * fade


save("trovao1.ogg", normalize(thunder(7.0, 0.45, 140, 7, 11), -17))    # perto: estalo e ronco forte
save("trovao2.ogg", normalize(thunder(8.5, 0.0, 110, 9, 22), -21))    # longe: só o ronco rolando
save("trovao3.ogg", normalize(thunder(6.0, 0.3, 170, 5, 33), -19))    # médio
