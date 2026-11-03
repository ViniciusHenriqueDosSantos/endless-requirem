import re, sys, base64, struct
from PIL import Image, ImageDraw, ImageFont
src, out = sys.argv[1], sys.argv[2]
text = open(src).read()
blocks = text.split("\n\n")
K = 0.25  # escala: 64px do jogo -> 16px na imagem
nodes = {}
for b in blocks:
    m = re.match(r'\[node name="([^"]+)"(?: type="([^"]+)")?(?: parent="([^"]+)")?.*?(?:instance=ExtResource\("([^"]+)"\))?\]', b)
    if not m: continue
    name, typ, parent, inst = m.groups()
    props = dict(re.findall(r'^(\w+) = (.+)$', b, re.M))
    key = name if parent in (None, ".") else f"{parent}/{name}"
    nodes[key] = dict(name=name, type=typ, parent=parent, inst=inst, props=props)
def vec(s):
    a = re.findall(r'-?[\d.]+', s.replace('Vector2', '')); return float(a[0]), float(a[1])
def gpos(key):
    n = nodes[key]; x, y = vec(n["props"].get("position", "Vector2(0,0)"))
    if n["parent"] not in (None, "."):
        px, py = gpos(n["parent"]); x += px; y += py
    return x, y
def tiles(name):
    data = base64.b64decode(re.search(r'PackedByteArray\("([^"]+)"\)', nodes[name]["props"]["tile_map_data"]).group(1))
    for i in range(2, len(data), 12):
        x, y, s, ax, ay, alt = struct.unpack('<hhHHHH', data[i:i+12]); yield x, y, ax
_all = [c for name in ("Floor", "Walls", "OuterWalls") if name in nodes for c in tiles(name)]
W, H = (max(c[0] for c in _all) + 2) * 16, (max(c[1] for c in _all) + 2) * 16
img = Image.new("RGB", (W + 260, H), (18, 20, 24)); d = ImageDraw.Draw(img)
try:
    f = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 11)
    fb = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 15)
except Exception:
    f = fb = ImageFont.load_default()
for x, y, a in tiles("Floor"):
    d.rectangle([x*16, y*16, x*16+15, y*16+15], fill=(222, 214, 196) if a == 0 else (214, 206, 188))
for key, n in nodes.items():
    if n["type"] != "ColorRect": continue
    cp = n["props"]; cc = [float(v) for v in re.findall(r'[\d.]+', cp["color"])]
    d.rectangle([float(cp["offset_left"])*K, float(cp["offset_top"])*K, float(cp["offset_right"])*K-1,
                 float(cp["offset_bottom"])*K-1], fill=tuple(int(v*255) for v in cc[:3]))
for x, y, a in tiles("Walls"):
    d.rectangle([x*16, y*16, x*16+15, y*16+15], fill=(150, 152, 156) if a == 3 else (40, 43, 50))
for x, y, a in (tiles("OuterWalls") if "OuterWalls" in nodes else []):
    d.rectangle([x*16, y*16, x*16+15, y*16+15], fill=(150, 152, 156) if a == 3 else (40, 43, 50))
    d.rectangle([x*16, y*16, x*16+15, y*16+15], outline=(150, 70, 200))
# salas
for key, n in nodes.items():
    if n["inst"] == "room":
        x, y = gpos(key); w, h = vec(n["props"]["Size"])
        d.rectangle([x*K, y*K, (x+w)*K, (y+h)*K], outline=(80, 255, 150))
for key, n in nodes.items():
    p = n["props"]; inst = n["inst"]
    if inst in ("prop", "door") and "Size" in p:
        x, y = gpos(key); w, h = vec(p["Size"])
        c = [float(v) for v in re.findall(r'[\d.]+', p.get("Color", "Color(0.1,0.85,0.45,1)"))]
        if inst == "prop" and len(c) == 4 and c[3] == 0:
            tc = [float(v) for v in re.findall(r'[\d.]+', p.get("TextColor", "Color(0.35,1,0.55,1)"))]
            d.text(((x)*K, (y-h/2)*K), p.get("Text", "").strip('"'), fill=tuple(int(v*255) for v in tc[:3]), font=fb, anchor="mm"); continue
        rgb = tuple(int(v*255) for v in c[:3])
        if inst == "door":
            d.rectangle([(x-w/2)*K, (y-h)*K, (x+w/2)*K, y*K], fill=rgb, outline=(255, 255, 255))
        else:
            d.rectangle([(x-w/2)*K, (y-h)*K, (x+w/2)*K, y*K], fill=rgb, outline=(20, 20, 20))
    if inst == "pickup" and "StartsHidden" not in p:
        x, y = gpos(key); c = [float(v) for v in re.findall(r'[\d.]+', p["Color"])]
        d.ellipse([x*K-6, y*K-12, x*K+6, y*K], fill=tuple(int(v*255) for v in c[:3]), outline=(255, 255, 255))
    if inst == "boss_target":
        x, y = gpos(key); d.ellipse([x*K-8, y*K-18, x*K+8, y*K-2], fill=(230, 50, 50), outline=(255, 255, 255))
    if inst == "boss":
        x, y = gpos(key); d.rectangle([x*K-12, y*K-28, x*K+12, y*K], fill=(237, 140, 102), outline=(255, 255, 255))
        d.text((x*K, y*K-36), "TIA", fill=(255, 255, 255), font=f, anchor="mm")
    if inst == "librarian":
        x, y = gpos(key); d.rectangle([x*K-11, y*K-28, x*K+11, y*K], fill=(107, 122, 199), outline=(255, 255, 255))
        d.text((x*K, y*K-36), "BIBL.", fill=(255, 255, 255), font=f, anchor="mm")
    if inst == "lamp":
        x, y = gpos(key); d.ellipse([x*K-7, y*K-22, x*K+7, y*K-8], fill=(255, 230, 120), outline=(120, 100, 40))
    if n["type"] == "Marker2D" and key.startswith("Biblioteca/Patrulha"):
        x, y = gpos(key); d.ellipse([x*K-3, y*K-3, x*K+3, y*K+3], fill=(150, 160, 255))
    if inst == "paper_pile":
        x, y = gpos(key); d.rectangle([x*K-9, y*K-14, x*K+9, y*K], fill=(220, 214, 194), outline=(120, 110, 90))
    if inst == "area_trigger":
        x, y = gpos(key); w, h = vec(p["Size"])
        d.rectangle([x*K, y*K, (x+w)*K, (y+h)*K], fill=(40, 120, 70))
    if inst == "player":
        x, y = gpos(key); d.ellipse([x*K-6, y*K-6, x*K+6, y*K+6], fill=(255, 255, 255))
    if n["type"] == "Marker2D" and n["name"].startswith("Spawn"):
        x, y = gpos(key); d.text((x*K, y*K), "x", fill=(255, 90, 90), font=f, anchor="mm")
    if n["type"] == "Marker2D" and n["name"] == "Respawn":
        x, y = gpos(key); d.text((x*K, y*K), "R", fill=(120, 200, 255), font=f, anchor="mm")
# legendas
LABELS = {}
LABELS["fase_1"] = [("ENTRADA", 18, 57.5), ("PORTARIA", 24, 45.2), ("CALÇADA", 9, 63.5), ("CORREDOR", 27, 30), ("RU", 12, 27.3),
          ("CANTINA", 12, 12.2), ("ELEVADOR", 9, 1.3)]
LABELS["fase_2"] = [("BIBLIOTECA", 9, 11), ("HALL DAS CARONAS", 12, 36.5), ("CORREDOR", 26.5, 30), ("ACHADOS", 34.5, 16.6),
                    ("ENTRADA BIBL.", 21, 22.5), ("ELEVADOR (quebrado)", 21, 5.5), ("ESCADA", 26.5, 1.6),
                    ("CHEGADA", 21, 45.2), ("ESTUDO", 9, 3.4)]
LABELS["escada"] = [("4º ANDAR", 12, 12), ("COMEÇO", 4, 26.5)]
import os as _os
labels = LABELS.get(_os.path.splitext(_os.path.basename(src))[0], [])
for t, tx, ty in labels:
    d.text((tx*16, ty*16), t, fill=(255, 255, 255), font=fb, anchor="mm", stroke_width=3, stroke_fill=(0, 0, 0))
leg = [((255,255,255), "jogador (começo)"), ((220,214,194), "pilha de papel"), ((102,191,255), "crachá (sorteado)"), ((255,153,64), "marmita"),
       ((242,204,77), "catraca"), ((26,217,115), "porta (tranca)"), ((237,140,102), "Tia da Cantina"), ((107,122,199), "Bibliotecário"), ((255,230,120), "luminária"), ((230,50,50), "alvos do escudo"), ((40,120,70), "elevador (fim)"),
       ((255,90,90), "x  surgem fantasmas"), ((120,200,255), "R  volta se reprovado"), ((80,255,150), "contorno de sala"), ((150,70,200), "borda: fantasma não passa")]
lx = W + 16
d.text((lx, 20), _os.path.basename(src).replace(".tscn", "") + " — planta", fill=(255,255,255), font=fb)
for i, (c, t) in enumerate(leg):
    yy = 56 + i * 26
    d.rectangle([lx, yy, lx + 14, yy + 14], fill=c); d.text((lx + 22, yy), t, fill=(220, 220, 220), font=f)
steps = [] if "fase_1" not in src else ["1  atire nas pilhas: crachá", "2  passe a catraca", "3  suba e entre no RU", "4  marmita -> micro-ondas",
         "5  sobreviva 50 s", "6  limpe o corredor", "7  derrote a Tia", "8  escopeta + elevador"]
if steps: d.text((lx, 420), "CAMINHO", fill=(255,255,255), font=fb)
for i, t in enumerate(steps):
    d.text((lx, 448 + i * 22), t, fill=(200, 230, 210), font=f)
img = img.resize((img.width * 2, img.height * 2), Image.NEAREST)
img.save(out)
print(img.size)
