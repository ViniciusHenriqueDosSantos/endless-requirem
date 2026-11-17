# Gera levels/fase_3.tscn (4º andar: laboratórios) a partir da planta do João (FASE_3_NOVA).
# Uso: python3 tools/levelgen/gen_fase3.py levels/fase_3.tscn
#
# ESBOÇO (MVP 4a): só a planta, os móveis principais e a luz. As lutas e o roteiro entram depois.
# Lendo de baixo para cima:
#   entrada (escada e elevador desligado, embaixo à direita) -> corredor comprido para cima
#   -> Laboratório 1 (esquerda) -> Laboratório 2 (direita) -> hall estreito com dois elevadores (não usados)
#   -> área de convivência no alto (onde será a luta final): tatame, mesas, ping-pong, cadeiras e o pallet.
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from lib import Level, T, v, col, px, root_node, CONCRETE, WHITE, WOOD

L = Level()

# ---------- planta (tiles, retângulos inclusivos) ----------
# Convivência (a luta final), no formato do desenho AREA_BOSS_FINAL: um bloco principal com o tatame no canto de
# cima à esquerda, um recorte vazio no meio do lado esquerdo, um pedaço embaixo à esquerda e um avanço à direita.
CONV_PARTES = [(7, 0, 27, 28),      # bloco principal
               (0, 0, 6, 9),        # parte de cima à esquerda (o tatame)
               (0, 22, 6, 28),      # pedaço de baixo à esquerda
               (28, 10, 36, 18)]    # avanço à direita
CONV = (0, 0, 36, 28)          # retângulo que contém a convivência inteira
TATAME = (0, 0, 13, 9)         # canto de cima à esquerda, onde os alunos descansam
CORR = (4, 29, 7, 71)          # corredor (4 de largura), sai de baixo da convivência
HALL = (9, 31, 11, 38)         # hall estreito dos elevadores (direita)
LAB2 = (9, 43, 20, 53)         # laboratório 2 (direita)
LAB1 = (-9, 55, 2, 65)         # laboratório 1 (esquerda)
ENTRADA = (9, 64, 11, 71)      # chegada: escada e elevador (mesma largura do hall)

for r in CONV_PARTES + [CORR, HALL, LAB2, LAB1, ENTRADA]:
    L.add_floor(*r)
L.add_floor(8, 32, 8, 37)      # hall aberto para o corredor
L.add_floor(8, 44, 8, 45)      # porta do laboratório 2 (pequena, no alto da sala)
L.add_floor(3, 56, 3, 57)      # porta do laboratório 1 (pequena, no alto da sala)
L.add_floor(8, 65, 8, 70)      # entrada aberta para o corredor
L.build_walls()

# ---------- recursos ----------
L.res('Script', 'res://levels/Fase3.cs', 'fase3')
for name, path in [('player', 'player/player'), ('hud', 'hud/hud'), ('message_screen', 'message_screen/message_screen'),
                   ('room', 'room/room'), ('door', 'door/door'), ('item_slot', 'item_slot/item_slot')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
for name, path in [('night_overlay', 'night/night_overlay'), ('flicker_light', 'night/flicker_light'),
                   ('rain', 'weather/rain'), ('item_inspect', 'inspect/item_inspect')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
L.res('Texture2D', 'res://sprites/lights/tubo.png', 'light_tubo')

# ---------- arte do piso e das paredes (mesmo tileset das Fases 1 e 2) ----------
from tiling import h


def inside(r, x, y):
    return r[0] <= x <= r[2] and r[1] <= y <= r[3]


def material(x, y):
    if inside(LAB1, x, y) or inside(LAB2, x, y): return "ceramica"   # laboratórios: piso de cerâmica
    if any(inside(p, x, y) for p in CONV_PARTES): return "vinil"                              # convivência: vinílico
    return "granito"                                                   # corredor, hall e entrada


floor_mat = {c: material(*c) for c in L.floor}


def deco_rule(c, style, below):
    x, y = c
    r = h("deco3", x, y)
    if style != "interno": return None
    if y == -1: return "janelao_quebrado" if r < 0.3 else "janelao"       # janelões no fundo da convivência
    if below == "granito":
        if r < 0.10: return "saida"
        if r < 0.18: return "extintor"
        if r < 0.30: return "persiana"
        if r < 0.45: return "rachada"
        if r < 0.60: return "mofo"
        return None
    if r < 0.2: return "rachada"
    if r < 0.4: return "mofo"
    return None


DECALS = [
    (lambda m: m == "vinil", [("papel", 2, 0.04), ("rachadura", 3, 0.02), ("mancha", 2, 0.02), ("poca", 2, 0.015)]),
    (lambda m: m == "granito", [("papel", 2, 0.05), ("rachadura", 3, 0.05), ("mancha", 2, 0.03), ("poca", 2, 0.03)]),
    (lambda m: m == "ceramica", [("papel", 2, 0.06), ("rachadura", 3, 0.04), ("vidro", 1, 0.03), ("mancha", 2, 0.03)]),
]
L.tiled_layers("fase1", floor_mat, deco_rule, decal_rules=DECALS,
               background=lambda x, y: f"laje{int(h('l', x, y) * 3)}")

CHAIR = col(0.2, 0.2, 0.22)
BOTTOM = (CONV[3] + 1) * T     # y da parede de baixo da convivência (pés dos móveis encostados nela)

# ---------- convivência ----------
conv = (CONV[0] * T, CONV[1] * T)
L.node("Convivencia", inst="room", props={"position": v(*conv), "Size": v((CONV[2] - CONV[0] + 1) * T,
       (CONV[3] - CONV[1] + 1) * T), "Waves": "0"})
# tatame: placas de EVA azuis e verdes no chão (2x2 tiles cada)
L.node("Tatame", type="Node2D")
for ty in range(TATAME[1], TATAME[3] + 1, 2):
    for tx in range(TATAME[0], TATAME[2] + 1, 2):
        w = min(2, TATAME[2] - tx + 1); hh = min(2, TATAME[3] - ty + 1)
        c = col(0.16, 0.32, 0.55, 1) if ((tx + ty) // 2) % 2 == 0 else col(0.18, 0.45, 0.3, 1)
        L.node(f"Placa_{tx}_{ty}", "Tatame", type="ColorRect", props={"z_index": "-5", "offset_left": f"{tx * T + 2}.0",
               "offset_top": f"{ty * T + 2}.0", "offset_right": f"{(tx + w) * T - 2}.0",
               "offset_bottom": f"{(ty + hh) * T - 2}.0", "mouse_filter": "2", "color": c})
L.prop("PlacaTatame", ".", 4 * T + 32, 2 * T + 40, 260, 30, col(0, 0, 0, 0), "TATAME", font=16,
       text_color=col(0.75, 0.9, 1), solid=False, z=1)

# canto de cima à direita: três mesas do mesmo estilo (a primeira com 3 cadeiras, as outras com 4)
for i, (tx, chairs) in enumerate([(15, "NSO"), (20, "NSOL"), (25, "NSOL")]):
    cx, cy = tx * T + 32, 3 * T + 48
    L.prop(f"Mesa{i+1}", ".", cx, cy, 96, 64, WOOD, depth=40, sprite="mesa_cantina", folder="props/fase1")
    spots = {"N": (cx, cy - 72), "S": (cx, cy + 60), "O": (cx - 76, cy + 8), "L": (cx + 76, cy + 8)}
    for side in chairs:
        x, y = spots[side]
        L.prop(f"Mesa{i+1}Cadeira{side}", ".", x, y, 36, 46, CHAIR, depth=22, sprite="cadeira_espera")

# duas mesas de ping-pong: mais para baixo e para a direita (sem encostar na parede de baixo)
for i, ty in enumerate([19, 22]):
    L.prop(f"PingPong{i+1}", ".", 20 * T, ty * T + 16, 200, 104, col(0.1, 0.38, 0.28), "PING-PONG", depth=64,
           font=11, text_color=col(0.9, 0.95, 0.9), inner=col(0.95, 0.95, 0.95))

# parede de baixo: o pallet com almofadas à esquerda do corredor; à direita, uma fileira de cadeiras (iguais às das
# mesas) todas coladas, viradas para o ping-pong e, mais para a direita, as lixeiras
L.prop("Pallet", ".", 2 * T, BOTTOM - 4, 3 * T + 16, 64, col(0.55, 0.42, 0.26), depth=40,
       inner=col(0.75, 0.35, 0.3))
for i in range(22):
    L.prop(f"CadeiraFileira{i+1}", ".", 10 * T + 20 + i * 40, BOTTOM - 4, 36, 46, CHAIR, depth=22,
           sprite="cadeira_espera")
L.prop("LixeirasConv", ".", 26 * T + 16, BOTTOM - 4, 84, 48, col(0, 0, 0, 0), solid=False, sprite="lixeiras",
       folder="props/fase1")

# ---------- corredor ----------
L.prop("LixeirasCorredor", ".", 7 * T + 24, 41 * T, 84, 48, col(0, 0, 0, 0), solid=False, sprite="lixeiras",
       folder="props/fase1")
L.prop("Bebedouro", ".", 4 * T + 20, 51 * T, 32, 64, col(0.7, 0.72, 0.75), depth=24, sprite="bebedouro",
       folder="props/fase1")

# ---------- hall estreito: dois elevadores na parede da direita (não são usados) ----------
for i, feet in enumerate([34, 38]):
    L.prop(f"ElevadorHall{i+1}", ".", HALL[2] * T + 40, (feet + 1) * T, 48, 3 * T, CONCRETE, depth=3 * T, solid=False,
           sprite="elevador", flip=True)
    L.node(f"AvisoElevadorHall{i+1}", inst="item_slot", props={"position": v(HALL[2] * T, (feet - 0.5) * T),
           "Size": v(140, 200), "RequiredItem": '"nunca"', "MissingMessage": '"ELEVADOR - sem energia neste andar."'})
L.prop("PlacaHall", ".", 10 * T + 32, HALL[1] * T + 4, 160, 26, col(0, 0, 0, 0), "ELEVADORES", font=13,
       text_color=col(0.9, 0.9, 0.85), solid=False, z=1)

# ---------- laboratórios: duas fileiras verticais de computadores, com cadeiras do lado ----------
PC = col(0.75, 0.75, 0.72)


def laboratorio(name, r, door_x, door_y, label_x):
    ox, oy = r[0] * T, r[1] * T
    L.node(name, inst="room", props={"position": v(ox, oy), "Size": v((r[2] - r[0] + 1) * T, (r[3] - r[1] + 1) * T),
           "Waves": "0"})
    width = r[2] - r[0] + 1
    top, bottom = r[1] + 2, r[3] - 1          # bancadas deixam um tile livre em cima e embaixo para circular
    for i, frac in enumerate([1 / 3, 2 / 3]):
        bx = r[0] * T + width * T * frac
        L.prop(f"Bancada{i+1}", name, bx - ox, bottom * T - oy, 72, (bottom - top) * T, PC, "", depth=(bottom - top) * T)
        # cadeiras dos dois lados da bancada, uma a cada computador
        for j, ty in enumerate(range(top, bottom, 2)):
            for side, dx in (("E", -64), ("D", 64)):
                if (j + (0 if side == "E" else 1)) % 3 == 2: continue   # algumas cadeiras faltando (abandono)
                L.prop(f"Cadeira{i+1}{side}{j+1}", name, bx + dx - ox, ty * T + 56 - oy, 36, 46, CHAIR, depth=22,
                       sprite="cadeira_espera")
    # lousas coladas na parede de cima
    for i, frac in enumerate([0.27, 0.73]):
        L.prop(f"Lousa{i+1}", name, r[0] * T + width * T * frac - ox, r[1] * T + 4 - oy, 2 * T + 48, 52,
               col(0.12, 0.3, 0.22), solid=False, sprite="lousa")
    L.prop(f"Placa{name}", ".", label_x * T, r[1] * T - 40, 240, 26, col(0, 0, 0, 0), "LABORATÓRIO " + name[-1],
           font=13, text_color=col(0.7, 0.95, 1), solid=False, z=1)
    L.node(f"Porta{name}", inst="door", props={"position": v(door_x * T + 32, (door_y + 2) * T), "Size": v(64, 2 * T),
           "Color": col(0.4, 0.8, 1, 1)})


laboratorio("Lab1", LAB1, 3, 56, -3.5)
laboratorio("Lab2", LAB2, 8, 44, 15)

# ---------- entrada: escada e o elevador ao lado (desligado), na parede da direita ----------
L.prop("ElevadorEntrada", ".", ENTRADA[2] * T + 40, 67 * T, 48, 3 * T, CONCRETE, depth=3 * T, solid=False,
       sprite="elevador_quebrado", flip=True)
L.node("AvisoElevadorEntrada", inst="item_slot", props={"position": v(ENTRADA[2] * T, 65.5 * T), "Size": v(140, 200),
       "RequiredItem": '"nunca"', "MissingMessage": '"ELEVADOR - fora de serviço. Pelo menos a escada chegou aqui."'})
L.prop("PortaEscada", ".", ENTRADA[2] * T + 40, 71 * T, 48, 3 * T, col(0.5, 0.42, 0.3), depth=3 * T, solid=False,
       sprite="porta_escada", flip=True)
L.prop("Placa4Andar", ".", 10 * T + 32, ENTRADA[1] * T + 24, 160, 26, col(0, 0, 0, 0), "4º ANDAR", font=14,
       text_color=col(0.95, 0.9, 0.6), solid=False, z=1)

# ---------- noite e chuva (igual à Fase 2: tudo dentro do prédio) ----------
L.node("Noite", type="CanvasModulate", groups=["night"], props={"color": col(0.11, 0.12, 0.2, 1)})
L.node("NightOverlay", inst="night_overlay")
L.node("Chuva", inst="rain", props={"AlwaysInside": "true", "LightningInterval": v(16, 34)})
STEADY, FLICKER, BROKEN, PULSE = 0, 1, 2, 3
TUBO = col(0.82, 0.92, 1)
L.node("Luzes", type="Node2D")
_lights = []


def light(name, x, y, color, scale, energy, mode, tube=True):
    _lights.append(name)
    p = {"position": v(x, y), "color": color, "texture_scale": f"{scale:g}", "BaseEnergy": f"{energy:g}",
         "Behavior": str(mode), "Seed": str(len(_lights) + 300)}
    if tube: p["texture"] = 'ExtResource("light_tubo")'
    L.node(name, "Luzes", inst="flicker_light", props=p)


for i, (ty, mode) in enumerate([(32, FLICKER), (38, BROKEN), (44, STEADY), (50, FLICKER), (56, BROKEN), (62, FLICKER),
                                (68, STEADY)]):
    light(f"Corredor{i+1}", 6 * T, ty * T, TUBO, 2.0, 0.8, mode)
for i, (tx, ty, mode) in enumerate([(5, 4, STEADY), (20, 4, BROKEN), (12, 13, FLICKER), (22, 14, STEADY),
                                    (32, 14, FLICKER), (20, 21, BROKEN), (12, 24, FLICKER), (3, 25, STEADY)]):
    light(f"Convivencia{i+1}", tx * T, ty * T, TUBO, 2.2, 0.85, mode)
light("Hall", 10 * T + 32, 35 * T, TUBO, 1.6, 0.8, BROKEN)
for name, r in (("Lab1", LAB1), ("Lab2", LAB2)):
    for i, ty in enumerate([r[1] + 3, r[3] - 3]):
        light(f"{name}_{i+1}", (r[0] + r[2] + 1) * T / 2, ty * T, col(0.6, 0.85, 1), 2.0, 0.8, FLICKER if i else BROKEN)
light("Entrada", 10 * T + 32, 68 * T, TUBO, 1.8, 0.8, STEADY)
light("ElevadorEntrada", ENTRADA[2] * T + 16, 65 * T, col(1, 0.15, 0.1), 1.2, 0.9, PULSE, tube=False)
light("PlacaEscada", ENTRADA[2] * T + 16, 69 * T, col(0.2, 1, 0.45), 0.8, 0.8, STEADY, tube=False)

# ---------- jogador / interface ----------
L.node("Player", inst="player", props={"position": v(10 * T + 32, 69 * T + 32), "StartWithAllWeapons": "false"})
L.node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
L.node("MessageScreen", inst="message_screen")
L.node("ItemInspect", inst="item_inspect")

paths = {"Player": "Player", "Hud": "Hud", "Screen": "MessageScreen", "Lab1": "Lab1", "Lab2": "Lab2",
         "Convivencia": "Convivencia"}
L.write(sys.argv[1], root_node("Fase3", "fase3", paths))
print("chão", len(L.floor), "paredes dentro", len(L.inner_walls), "fora", len(L.outer_walls))
