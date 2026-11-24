# Gera os sons da Fase 3 (sounds/fase3/): alarme do 4º andar e os bipes das telas (senha, fios, computador).
# Tudo sintetizado com numpy (sem gravação de terceiros).
#   python tools/audio/gen_fase3.py
#   alarme.wav  sirene de dois tons (loop de 2 s)
#   bip.wav     tecla do teclado de senha
#   erro.wav    senha errada (zumbido grave)
#   ok.wav      senha certa / tarefa concluída (dois tons subindo)
#   fio.wav     fio encaixado (estalo elétrico curto)
#   clique.wav  clique de link no computador
#   camera.wav  as câmeras do corredor apitando (três bipes agudos) logo antes do alarme
import os
import numpy as np
from scipy.io import wavfile

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "sounds", "fase3")
rng = np.random.default_rng(3003)


def t(seconds):
    return np.arange(int(SR * seconds)) / SR


def env(n, attack=0.005, release=0.03):
    e = np.ones(n)
    a, r = int(SR * attack), int(SR * release)
    if a: e[:a] = np.linspace(0, 1, a)
    if r: e[-r:] = np.linspace(1, 0, r)
    return e


def square(freq, x):
    return np.sign(np.sin(2 * np.pi * np.cumsum(np.broadcast_to(freq, x.shape)) / SR))


def save(name, x, gain=0.8):
    x = x / (np.abs(x).max() + 1e-9) * gain
    os.makedirs(OUT, exist_ok=True)
    wavfile.write(os.path.join(OUT, name), SR, (x * 32767).astype(np.int16))


# sirene: dois tons alternando (alto/baixo), timbre de alarme de prédio, emenda perfeita no loop
x = t(2.0)
freq = np.where((x % 1.0) < 0.5, 880.0, 660.0)
siren = 0.6 * square(freq, x) + 0.4 * np.sin(2 * np.pi * np.cumsum(freq) / SR)
siren = np.convolve(siren, np.ones(6) / 6, mode="same")          # tira a aspereza
save("alarme.wav", siren, 0.55)

x = t(0.07)
save("bip.wav", np.sin(2 * np.pi * 1320 * x) * env(len(x), 0.002, 0.03), 0.5)

x = t(0.35)
buzz = square(np.full(len(x), 110.0), x) * 0.7 + square(np.full(len(x), 116.0), x) * 0.3
save("erro.wav", buzz * env(len(x), 0.005, 0.08), 0.5)

a, b = t(0.12), t(0.22)
ok = np.concatenate([np.sin(2 * np.pi * 784 * a) * env(len(a), 0.003, 0.02),
                     np.sin(2 * np.pi * 1175 * b) * env(len(b), 0.003, 0.12)])
save("ok.wav", ok, 0.55)

x = t(0.12)
zap = rng.standard_normal(len(x)) * np.exp(-x * 40) + 0.5 * np.sin(2 * np.pi * 2400 * x) * np.exp(-x * 60)
save("fio.wav", zap, 0.5)

x = t(0.03)
save("clique.wav", rng.standard_normal(len(x)) * np.exp(-x * 200), 0.35)
beeps = []
for k in range(3):
    x = t(0.09)
    beeps += [square(np.full(len(x), 1760.0), x) * 0.5 * env(len(x), 0.002, 0.02), np.zeros(int(SR * 0.07))]
save("camera.wav", np.concatenate(beeps), 0.45)
print("sons em", os.path.abspath(OUT))
