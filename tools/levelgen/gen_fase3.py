# Gera levels/fase_3.tscn (4º andar: laboratórios) a partir da planta do João (FASE_3_NOVA).
# Uso: python3 tools/levelgen/gen_fase3.py levels/fase_3.tscn
#
# ESBOÇO (MVP 4a): só a planta, os móveis principais e a luz. As lutas e o roteiro entram depois.
# Escala: 1 tile = 20 px do desenho do João. Lendo de baixo para cima:
#   entrada (chega pela escada, embaixo à direita) -> corredor comprido para cima
#   -> Laboratório 1 (esquerda) -> Laboratório 2 (direita) -> hall com o outro elevador (direita, não usado)
#   -> área de convivência no alto: tatame, duas mesas de ping-pong, sofá e mesa com cadeiras.
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from lib import Level, T, v, col, px, root_node, CONCRETE, WHITE, WOOD

L = Level()

# ---------- planta (tiles, retângulos inclusivos) ----------
CONV = (0, 0, 21, 13)          # área de convivência (x0, y0, x1, y1)
TATAME = (0, 0, 8, 4)          # canto de cima à esquerda, onde os alunos descansam
CORR = (4, 14, 7, 55)          # corredor (4 de largura), sai de baixo da convivência
HALL = (9, 17, 13, 25)         # hall do outro elevador (direita)
LAB2 = (9, 32, 14, 40)         # laboratório 2 (direita)
LAB1 = (-3, 40, 2, 48)         # laboratório 1 (esquerda)
ENTRADA = (9, 51, 12, 55)      # chegada pela escada (embaixo à direita)
DIVISOES = [21, 30]            # linhas onde o corredor muda de trecho (as duas faixas do desenho)

for r in (CONV, CORR, HALL, LAB2, LAB1, ENTRADA):
    L.add_floor(*r)
L.add_floor(8, 18, 8, 24)      # hall aberto para o corredor
L.add_floor(8, 35, 8, 37)      # porta do laboratório 2
L.add_floor(3, 43, 3, 45)      # porta do laboratório 1
L.add_floor(8, 52, 8, 54)      # entrada aberta para o corredor
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
    if inside(CONV, x, y): return "vinil"                              # convivência: vinílico
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

# ---------- convivência ----------
conv = (CONV[0] * T, CONV[1] * T)
L.node("Convivencia", inst="room", props={"position": v(*conv), "Size": v((CONV[2] - CONV[0] + 1) * T,
       (CONV[3] - CONV[1] + 1) * T), "Waves": "0"})
# tatame: placas de EVA azuis e verdes no chão (2x2 tiles cada), com a borda mais clara
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
# duas mesas de ping-pong no meio (baixas: dá para atirar por cima)
for i, ty in enumerate([6, 8]):
    L.prop(f"PingPong{i+1}", ".", 12 * T, ty * T + (16 if i == 0 else 40), 200, 104, col(0.1, 0.38, 0.28),
           "PING-PONG", depth=64, font=11, text_color=col(0.9, 0.95, 0.9), inner=col(0.95, 0.95, 0.95))
# sofá no canto de baixo à esquerda
L.prop("Sofa", ".", 2 * T, 11 * T + 8, 128, 56, col(0.4, 0.25, 0.2), depth=40, sprite="sofa", folder="props/fase1")
# canto de cima à direita: mesa com três cadeiras e uma TV na parede
L.prop("MesaConv", ".", 16 * T + 32, 3 * T + 48, 96, 64, WOOD, depth=40, sprite="mesa_cantina", folder="props/fase1")
for i, (x, y) in enumerate([(16 * T + 32, 2 * T + 40), (16 * T + 32, 4 * T + 60), (15 * T + 16, 3 * T + 56)]):
    L.prop(f"CadeiraConv{i+1}", ".", x, y, 36, 46, col(0.2, 0.2, 0.22), depth=22, sprite="cadeira_espera")
L.prop("TV", ".", 21 * T + 40, 4 * T + 40, 16, 110, col(0.05, 0.05, 0.07), "", depth=24, solid=False)

# ---------- corredor ----------
# as duas faixas do desenho: por enquanto só uma soleira no chão (portas corta-fogo abertas)
for i, ty in enumerate(DIVISOES):
    L.node(f"Divisao{i+1}", type="ColorRect", props={"z_index": "-4", "offset_left": f"{CORR[0] * T}.0",
           "offset_top": f"{ty * T + 24}.0", "offset_right": f"{(CORR[2] + 1) * T}.0", "offset_bottom": f"{ty * T + 40}.0",
           "mouse_filter": "2", "color": col(0.45, 0.42, 0.38, 1)})
L.prop("LixeirasCorredor", ".", 7 * T + 24, 27 * T, 84, 48, col(0, 0, 0, 0), solid=False, sprite="lixeiras",
       folder="props/fase1")
L.prop("Bebedouro", ".", 4 * T + 20, 34 * T, 32, 64, col(0.7, 0.72, 0.75), depth=24, sprite="bebedouro",
       folder="props/fase1")

# ---------- hall do outro elevador (não é usado na fase) ----------
L.prop("ElevadorHall", ".", 11 * T + 32, 17 * T + 8, 128, 64, CONCRETE, solid=False, sprite="porta_elevador",
       folder="props/fase1")
L.node("AvisoElevadorHall", inst="item_slot", props={"position": v(11 * T + 32, 18 * T), "Size": v(220, 120),
       "RequiredItem": '"nunca"', "MissingMessage": '"ELEVADOR - sem energia neste andar."'})
L.prop("PlacaHall", ".", 11 * T, 25 * T + 40, 200, 26, col(0, 0, 0, 0), "ELEVADORES", font=13,
       text_color=col(0.9, 0.9, 0.85), solid=False, z=1)

# ---------- laboratórios ----------
def bancadas(name, r, parent):
    """Bancadas de computador em fileiras (baixas: cobrem o jogador, mas dá para atirar por cima)."""
    ox, oy = r[0] * T, r[1] * T
    n = 0
    for ty in range(r[1] + 2, r[3], 3):
        n += 1
        L.prop(f"Bancada{n}", parent, (r[0] + r[2] + 1) * T / 2 - ox, ty * T + 40 - oy, (r[2] - r[0] - 1) * T, 56,
               col(0.75, 0.75, 0.72), "PCs", depth=40, font=10, text_color=col(0.2, 0.25, 0.3))


for name, r, door_y, label_x in [("Lab1", LAB1, 44, -0.5), ("Lab2", LAB2, 36, 11.5)]:
    L.node(name, inst="room", props={"position": v(r[0] * T, r[1] * T), "Size": v((r[2] - r[0] + 1) * T,
           (r[3] - r[1] + 1) * T), "Waves": "0"})
    bancadas(name, r, name)
    L.prop(f"Placa{name}", ".", label_x * T + 32, r[1] * T + 24, 220, 26, col(0, 0, 0, 0),
           "LABORATÓRIO " + name[-1], font=13, text_color=col(0.7, 0.95, 1), solid=False, z=1)
    L.node(f"Porta{name}", inst="door", props={"position": v((3 if name == "Lab1" else 8) * T + 32, (door_y + 2) * T),
           "Size": v(64, 3 * T), "Color": col(0.4, 0.8, 1, 1)})

# ---------- entrada pela escada ----------
L.prop("PortaEscada", ".", 12 * T + 40, 55 * T, 48, 3 * T, col(0.5, 0.42, 0.3), depth=3 * T, tall=True,
       sprite="porta_escada")
L.prop("Placa4Andar", ".", 10 * T + 32, 51 * T + 24, 160, 26, col(0, 0, 0, 0), "4º ANDAR", font=14,
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


for i, (ty, mode) in enumerate([(17, FLICKER), (23, BROKEN), (29, STEADY), (35, FLICKER), (41, BROKEN), (47, FLICKER),
                                (53, STEADY)]):
    light(f"Corredor{i+1}", 6 * T, ty * T, TUBO, 2.0, 0.8, mode)
for i, (tx, ty, mode) in enumerate([(4, 3, STEADY), (12, 7, FLICKER), (17, 3, BROKEN), (5, 10, FLICKER)]):
    light(f"Convivencia{i+1}", tx * T, ty * T, TUBO, 2.2, 0.85, mode)
light("Hall", 11 * T, 21 * T, TUBO, 1.8, 0.8, BROKEN)
light("Lab1", 0, 44 * T, col(0.6, 0.85, 1), 2.0, 0.8, FLICKER)
light("Lab2", 12 * T, 36 * T, col(0.6, 0.85, 1), 2.0, 0.8, FLICKER)
light("Entrada", 10 * T + 32, 53 * T, TUBO, 1.6, 0.8, STEADY)
light("ElevadorHall", 11 * T + 32, 18 * T, col(1, 0.15, 0.1), 1.2, 0.9, PULSE, tube=False)
light("TV", 21 * T + 16, 4 * T, col(0.4, 0.6, 1), 1.0, 0.7, FLICKER, tube=False)

# ---------- jogador / interface ----------
L.node("Player", inst="player", props={"position": v(10 * T + 32, 53 * T + 32), "StartWithAllWeapons": "false"})
L.node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
L.node("MessageScreen", inst="message_screen")
L.node("ItemInspect", inst="item_inspect")

paths = {"Player": "Player", "Hud": "Hud", "Screen": "MessageScreen", "Lab1": "Lab1", "Lab2": "Lab2",
         "Convivencia": "Convivencia"}
L.write(sys.argv[1], root_node("Fase3", "fase3", paths))
print("chão", len(L.floor), "paredes dentro", len(L.inner_walls), "fora", len(L.outer_walls))
