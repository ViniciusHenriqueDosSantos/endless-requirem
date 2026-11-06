# Prévia: repete cada piso num quadrado e amplia 4x (como fica no jogo). Uso: python3 preview.py saida.png
import json, sys, os
from PIL import Image
from pix import ROOT
names = json.load(open(os.path.join(ROOT, "tools/levelgen/tiles_fase1.json")))["tiles"]
atlas = Image.open(os.path.join(ROOT, "sprites/tiles/fase1_tiles.png"))
def t(n):
    c, r = names[n]; return atlas.crop((c*16, r*16, c*16+16, r*16+16))
mats = ["bloquete", "calcada", "asfalto", "sextavado", "praca", "granito", "ceramica"]
W = 6
out = Image.new("RGBA", (len(mats) * (W*16 + 4), W*16), (20, 20, 25, 255))
import random
for i, m in enumerate(mats):
    r = random.Random(i)
    for y in range(W):
        for x in range(W):
            out.paste(t(f"{m}{r.choice([0,0,1,2,3])}"), (i*(W*16+4) + x*16, y*16))
out = out.resize((out.width*4, out.height*4), Image.NEAREST)
out.save(sys.argv[1] if len(sys.argv) > 1 else "/tmp/floors_preview.png")
