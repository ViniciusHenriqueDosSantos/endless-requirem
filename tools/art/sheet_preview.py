# Junta PNGs de uma pasta numa prancha ampliada (para revisar). Uso: python3 sheet_preview.py pasta saida.png [escala]
import sys, os
from PIL import Image
d, out = sys.argv[1], sys.argv[2]; k = int(sys.argv[3]) if len(sys.argv) > 3 else 3
ims = [(f, Image.open(os.path.join(d, f)).convert("RGBA")) for f in sorted(os.listdir(d)) if f.endswith(".png")]
W = 1500; x = y = 0; rowh = 0; pos = []
for f, im in ims:
    w, h = im.width * k, im.height * k
    if x + w > W: x = 0; y += rowh + 8; rowh = 0
    pos.append((im, x, y)); x += w + 8; rowh = max(rowh, h)
sheet = Image.new("RGBA", (W, y + rowh + 4), (64, 66, 72, 255))
for im, x, y in pos:
    sheet.alpha_composite(im.resize((im.width * k, im.height * k), Image.NEAREST), (x, y))
sheet.save(out)
