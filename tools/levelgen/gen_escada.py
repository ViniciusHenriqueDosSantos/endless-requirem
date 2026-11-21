# Gera levels/escada.tscn: a escada do prédio (fim da Fase 2), em zigue-zague, vista de cima.
# 8 lances (2 por andar), do térreo (0) ao 4º andar. Cada lance sobe; no patamar você vira para o outro lado.
# Estilo "Temple Run": os degraus desabam atrás do jogador (roteiro em levels/Escada.cs). Sem fantasmas.
# Um obstáculo por lance: barricada (1 tiro de Canhão), degrau quebrado, lâmpada caindo, extintor rolando
# e lance pela metade. A porta do 4º andar só aparece no 4º andar.
# Uso: python3 tools/levelgen/gen_escada.py levels/escada.tscn
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from lib import Level, T, v, col, px, root_node, CONCRETE

L = Level()
FL = 8                      # comprimento de cada lance (tiles)
LAND = 3                    # altura de cada patamar (tiles)
COLS = [(0, 2), (6, 8)]     # as duas colunas de lances (x0, x1): A (esquerda) e B (direita)
WIDTH = (0, 8)              # largura dos patamares
CX = [(a + b + 1) / 2 for a, b in COLS]   # meio de cada coluna (tiles)
MID = (WIDTH[0] + WIDTH[1] + 1) / 2

# ---------- planta (de baixo para cima; y fica negativo subindo) ----------
L.add_floor(WIDTH[0], -2, WIDTH[1], 0)          # patamar do térreo (começo)
flights, landings = [], []
top = -3
for k in range(8):
    a, b = COLS[k % 2]
    flights.append((a, top - FL + 1, b, top))                 # lance k (x0, y0, x1, y1)
    L.add_floor(*flights[-1])
    landings.append((WIDTH[0], top - FL - LAND + 1, WIDTH[1], top - FL))
    L.add_floor(*landings[-1])
    top = top - FL - LAND
last = landings[-1]
EXIT = (3, last[1] - 4, 5, last[1] - 2)          # patamar da saída (atrás da porta do 4º andar)
L.add_floor(3, last[1] - 1, 5, last[1] - 1)       # vão da porta
L.add_floor(*EXIT)
L.build_walls()

# ---------- recursos ----------
L.res('Script', 'res://levels/Escada.cs', 'escada')
for name, path in [('player', 'player/player'), ('hud', 'hud/hud'), ('message_screen', 'message_screen/message_screen'),
                   ('area_trigger', 'area_trigger/area_trigger'), ('door', 'door/door')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
for name, path in [('night_overlay', 'night/night_overlay'), ('flicker_light', 'night/flicker_light'),
                   ('rain', 'weather/rain')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
L.res('Texture2D', 'res://sprites/lights/tubo.png', 'light_tubo')
for name in ['CollapseFloor', 'Barricade', 'FallingLamp', 'RollingExtinguisher']:
    L.res('Script', f'res://objects/escada/{name}.cs', name)
L.res('AudioStream', 'res://sounds/escada/desabamento.wav', 'som_desabamento')
tex = lambda n: L.tex(n, "props/escada")

# ---------- arte: degraus com ranhuras nos lances, granito nos patamares, tudo parede interna ----------
from tiling import h
flight_cells = {(x, y) for f in flights for x in range(f[0], f[2] + 1) for y in range(f[1], f[3] + 1)}
floor_mat = {c: ("rampa" if c in flight_cells else "granito") for c in L.floor}


def deco_rule(c, style, below):
    r = h("decoE", *c)
    if r < 0.10: return "saida"
    if r < 0.28: return "rachada"
    if r < 0.48: return "mofo"
    return None


DECALS = [(lambda m: True, [("rachadura", 3, 0.08), ("poca", 2, 0.04), ("papel", 2, 0.03), ("mancha", 2, 0.03)])]
L.tiled_layers("fase1", floor_mat, deco_rule, style_rule=lambda c: "interno", decal_rules=DECALS)
xs = [c[0] for c in L.floor]; ys = [c[1] for c in L.floor]
L.node("Fundo", type="ColorRect", props={"z_index": "-12", "offset_left": f"{(min(xs) - 12) * T}.0",
       "offset_top": f"{(min(ys) - 12) * T}.0", "offset_right": f"{(max(xs) + 13) * T}.0",
       "offset_bottom": f"{(max(ys) + 13) * T}.0", "mouse_filter": "2", "color": col(0.03, 0.035, 0.045, 1)})

# número do andar pintado no chão de cada patamar de andar (térreo = 0)
L.prop("Andar0", ".", MID * T, -T + 56, 96, 112, col(0, 0, 0, 0), solid=False, sprite="andar0", folder="props/escada",
       z=-7)
for f in range(1, 5):
    lnd = landings[2 * f - 1]
    L.prop(f"Andar{f}", ".", MID * T, (lnd[1] + 1) * T + 88, 96, 112, col(0, 0, 0, 0), solid=False,
           sprite=f"andar{f}", folder="props/escada", z=-7)

# ---------- caminho (meio da faixa) e o começo de cada andar ----------
path = [(CX[0], -1.0)]
floor_starts = [0.0]
for k in range(8):
    lnd = landings[k]
    ly = (lnd[1] + lnd[3] + 1) / 2
    x = CX[k % 2]
    path.append((x, ly))
    if k < 7:
        path.append((CX[(k + 1) % 2], ly))
path += [(MID, ly), (MID, EXIT[1] + 1.5)]


def s_at(i):   # distância ao longo do caminho até o ponto i (pixels)
    return sum(((path[j + 1][0] - path[j][0]) ** 2 + (path[j + 1][1] - path[j][1]) ** 2) ** 0.5
               for j in range(i)) * T


# o ponto do patamar do lance k é o índice 1 + 2k; os andares começam nos patamares dos lances 1, 3, 5 e 7
floor_starts += [s_at(1 + 2 * k) for k in (1, 3, 5, 7)]

# ---------- obstáculos (um por lance) ----------
L.node("Obstaculos", type="Node2D", props={"y_sort_enabled": "true"})


def barricada(name, k, row_from_top, variant=0):
    f = flights[k]
    L.node(name, "Obstaculos", type="StaticBody2D", props={
        "position": v(CX[k % 2] * T, (f[1] + row_from_top + 1) * T), "script": 'ExtResource("Barricade")',
        "Size": v(3 * T, 56), "Texture": tex(f"barricada{variant}"), "Health": "3.0"})


def buraco(name, k, x_from, row_from_top, w, hh, sprite):
    f = flights[k]
    x = (COLS[k % 2][0] + x_from + w / 2) * T
    L.prop(name, "Obstaculos", x, (f[1] + row_from_top + hh) * T, w * T, hh * T, col(0.02, 0.02, 0.03), depth=hh * T,
           sprite=sprite, folder="props/escada", z=-6)


def lampada(name, k, x_from, row_from_top):
    f = flights[k]
    L.node(name, "Obstaculos", type="Node2D", props={
        "position": v((COLS[k % 2][0] + x_from + 0.5) * T, (f[1] + row_from_top + 0.5) * T),
        "script": 'ExtResource("FallingLamp")', "LampTexture": tex("lampada"), "BrokenTexture": tex("lampada_quebrada")})


def extintor(name, k, x_from, trigger=520):
    f = flights[k]
    L.node(name, "Obstaculos", type="Area2D", props={
        "position": v((COLS[k % 2][0] + x_from + 0.5) * T, (f[1] + 0.5) * T), "script": 'ExtResource("RollingExtinguisher")',
        "Frame0": tex("extintor0"), "Frame1": tex("extintor1"), "Puff": tex("espuma"), "TriggerDistance": str(trigger)})


# lance 0: tranquilo (aprender a regra)
barricada("Barricada1", 1, 3, 0)                                     # lance 1: barricada
buraco("DegrauQuebrado1", 2, 0, 3, 2, 2, "degrau_quebrado")          # lance 2: degrau quebrado (passa pela direita)
lampada("Lampada1", 3, 0, 5); lampada("Lampada2", 3, 2, 2)           # lance 3: lâmpadas caindo
extintor("Extintor1", 4, 1)                                          # lance 4: extintor rolando
buraco("LanceMetade1", 5, 1, 1, 2, 5, "lance_metade")                # lance 5: lance pela metade + lâmpada
lampada("Lampada3", 5, 0, 2)
buraco("DegrauQuebrado2", 6, 1, 1, 2, 2, "degrau_quebrado")          # lance 6: degrau quebrado + barricada
barricada("Barricada2", 6, 5, 1)
extintor("Extintor2", 7, 0, 560); extintor("Extintor3", 7, 2, 300)   # lance 7: dois extintores

# ---------- começo, saída e o buraco que vem atrás ----------
L.node("Desabamento", type="Node2D", props={"z_index": "-6", "script": 'ExtResource("CollapseFloor")'})
f0 = flights[0]
L.node("Inicio", inst="area_trigger", props={"position": v(COLS[0][0] * T, (f0[3] - 1) * T), "Size": v(3 * T, 2 * T),
       "Color": col(0, 0, 0, 0)})
L.prop("PortaEscadaEntrada", ".", (WIDTH[1] + 1) * T - 24, T, 48, 3 * T, col(0.5, 0.42, 0.3), depth=3 * T, solid=False,
       sprite="porta_escada", flip=True)
L.node("PortaSaida", inst="door", props={"position": v(MID * T, last[1] * T), "Size": v(3 * T, 64), "StartsLocked": "true",
       "WallStyle": "true", "Color": col(0.9, 0.75, 0.3, 1)})
L.node("CoberturaSaida", type="ColorRect", props={"z_index": "20", "offset_left": f"{(EXIT[0] - 1) * T}.0",
       "offset_top": f"{(EXIT[1] - 1) * T}.0", "offset_right": f"{(EXIT[2] + 2) * T}.0",
       "offset_bottom": f"{(last[1] - 1) * T}.0", "mouse_filter": "2", "color": col(0.03, 0.035, 0.045, 1)})
L.node("Saida", inst="area_trigger", props={"position": v(EXIT[0] * T, EXIT[1] * T),
       "Size": v((EXIT[2] - EXIT[0] + 1) * T, (EXIT[3] - EXIT[1] + 1) * T), "Text": '"4º ANDAR"'})

# ---------- noite: luzes de emergência nos patamares e a chuva lá fora ----------
L.node("Noite", type="CanvasModulate", groups=["night"], props={"color": col(0.11, 0.12, 0.2, 1)})
L.node("NightOverlay", inst="night_overlay")
L.node("Chuva", inst="rain", props={"AlwaysInside": "true", "LightningInterval": v(12, 26)})
L.node("Luzes", type="Node2D")
for i, lnd in enumerate([(0, -2, 8, 0)] + landings):
    L.node(f"Emergencia{i}", "Luzes", inst="flicker_light", props={
        "position": v(MID * T, (lnd[1] + 1.5) * T), "color": col(1, 0.82, 0.55), "texture_scale": "2.2",
        "BaseEnergy": "0.85", "Behavior": str(1 if i % 2 else 2), "Seed": str(500 + i), "texture": 'ExtResource("light_tubo")'})
for k, f in enumerate(flights):
    L.node(f"Lance{k}", "Luzes", inst="flicker_light", props={
        "position": v(CX[k % 2] * T, (f[1] + 4) * T), "color": col(1, 0.82, 0.55), "texture_scale": "1.6",
        "BaseEnergy": "0.6", "Behavior": str(2 if k % 3 == 0 else 1), "Seed": str(520 + k), "texture": 'ExtResource("light_tubo")'})
L.node("LuzSaida", "Luzes", inst="flicker_light", props={"position": v(MID * T, (last[1] - 2) * T),
       "color": col(0.2, 1, 0.45), "texture_scale": "1.2", "BaseEnergy": "0.8", "Behavior": "0", "Seed": "540"})

L.node("Player", inst="player", props={"position": v(CX[0] * T, -T + 32), "StartWithAllWeapons": "false"})
L.node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
L.node("MessageScreen", inst="message_screen")

paths = {"Player": "Player", "Hud": "Hud", "Screen": "MessageScreen", "PortaSaida": "PortaSaida", "Saida": "Saida",
         "Inicio": "Inicio", "CoberturaSaida": "CoberturaSaida", "Desabamento": "Desabamento", "Obstaculos": "Obstaculos"}
root = root_node("Escada", "escada", paths)
root += "\nCaminho = PackedVector2Array(" + ", ".join(f"{x * T:g}, {y * T:g}" for x, y in path) + ")"
root += "\nInicioDosAndares = PackedFloat32Array(" + ", ".join(f"{s:g}" for s in floor_starts) + ")"
root += '\nSomDesabamento = ExtResource("som_desabamento")'
L.write(sys.argv[1], root)
print("chão", len(L.floor), "caminho", round(s_at(len(path) - 1) / T), "tiles", "andares", [round(s / T) for s in floor_starts])
