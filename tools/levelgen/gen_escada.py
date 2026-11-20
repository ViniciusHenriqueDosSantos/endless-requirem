# Gera levels/escada.tscn: escada circular vista de cima (fim da Fase 2).
# É uma volta só: o jogador dá voltas no mesmo anel e cada volta completa é um andar.
# Estilo "Temple Run": os degraus desabam atrás do jogador; barricadas (só o Canhão derruba) fecham a faixa;
# poucos fantasmas. A porta de saída só aparece no 4º andar (antes é parede). Roteiro em levels/Escada.cs.
# Uso: python3 tools/levelgen/gen_escada.py levels/escada.tscn
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from lib import Level, T, v, col, px, root_node, CONCRETE

L = Level()
W = 3                                   # largura do anel (tiles)
X0, Y0, X1, Y1 = 2, 5, 20, 19           # contorno de fora do anel
RING = [(X0, Y0, X1, Y0 + W - 1), (X0, Y1 - W + 1, X1, Y1), (X0, Y0, X0 + W - 1, Y1), (X1 - W + 1, Y0, X1, Y1)]
for r in RING:
    L.add_floor(*r)
L.add_floor(2, 20, 4, 22)               # patamar de entrada (embaixo, à esquerda)
L.add_floor(10, 4, 12, 4)               # porta de saída (em cima)
L.add_floor(9, 1, 13, 3)                # patamar do 4º andar
L.build_walls()

L.res('Script', 'res://levels/Escada.cs', 'escada')
for name, path in [('player', 'player/player'), ('hud', 'hud/hud'), ('message_screen', 'message_screen/message_screen'),
                   ('room', 'room/room'), ('area_trigger', 'area_trigger/area_trigger'), ('door', 'door/door')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
for name, path in [('night_overlay', 'night/night_overlay'), ('flicker_light', 'night/flicker_light'),
                   ('rain', 'weather/rain')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
L.res('Texture2D', 'res://sprites/lights/tubo.png', 'light_tubo')
L.res('Script', 'res://objects/escada/CollapseFloor.cs', 'collapse')

# ---------- arte: degraus com ranhuras (piso da rampa da Fase 1) e patamares de granito ----------
from tiling import h


def material(x, y):
    if any(r[0] <= x <= r[2] and r[1] <= y <= r[3] for r in RING): return "rampa"
    return "granito"


def deco_rule(c, style, below):
    if style != "interno": return None
    r = h("decoE", *c)
    if r < 0.12: return "saida"
    if r < 0.30: return "rachada"
    if r < 0.50: return "mofo"
    return None


floor_mat = {c: material(*c) for c in L.floor}
DECALS = [(lambda m: True, [("rachadura", 3, 0.08), ("poca", 2, 0.04), ("papel", 2, 0.03), ("mancha", 2, 0.03)])]
L.tiled_layers("fase1", floor_mat, deco_rule, style_rule=lambda c: "interno", decal_rules=DECALS,
               background=lambda x, y: f"laje{int(h('l', x, y) * 3)}")

# vão da escada (centro): escuro, dá para ver lá embaixo
L.node("Vao", type="ColorRect", props={"z_index": "-5", "offset_left": f"{(X0 + W) * T}.0",
       "offset_top": f"{(Y0 + W) * T}.0", "offset_right": f"{(X1 - W + 1) * T}.0",
       "offset_bottom": f"{(Y1 - W + 1) * T}.0", "mouse_filter": "2", "color": col(0.03, 0.035, 0.045, 1)})
L.prop("PlacaVao", ".", 11 * T + 32, 13 * T, 360, 40, col(0, 0, 0, 0), "vão da escada", font=18,
       text_color=col(0.3, 0.34, 0.33), solid=False, z=1)

# o buraco que vem desabando atrás do jogador (desenhado pelo roteiro) e onde nascem as barricadas
L.node("Desabamento", type="Node2D", props={"z_index": "-6", "script": 'ExtResource("collapse")'})
L.node("Barricadas", type="Node2D", props={"y_sort_enabled": "true"})

# sala única: poucos fantasmas surgindo enquanto o jogador sobe (Escada.cs ajusta por andar)
L.node("Subida", inst="room", props={"position": v(X0 * T, Y0 * T), "Size": v((X1 - X0 + 1) * T, (Y1 - Y0 + 1) * T),
       "Waves": "0", "SurviveTime": "9999.0", "SurviveSpawnInterval": "4.0", "SurviveMaxAlive": "2",
       "SpawnAnywhere": "true", "EnemyHealthScale": "0.6", "EnemySpeedScale": "1.1",
       "RespawnPoint": 'NodePath("Respawn")'}, node_paths=["RespawnPoint"])
L.node("Respawn", "Subida", type="Marker2D", props={"position": v(3 * T + 32 - X0 * T, 21 * T + 32 - Y0 * T)})

# setas pintadas no chão indicando o sentido da subida
for i, (x, y, t) in enumerate([(3, 15, "↑"), (3, 9, "↑"), (6, 6, "→"), (16, 6, "→"), (19, 9, "↓"), (19, 15, "↓"),
                               (16, 18, "←"), (6, 18, "←")]):
    L.prop(f"Seta{i}", ".", x * T + 32, y * T + 48, 64, 64, col(0, 0, 0, 0), t, font=40,
           text_color=col(0.85, 0.8, 0.6, 0.55), solid=False, z=-4)

# saída do 4º andar: parece parede até chegar lá; o patamar fica coberto
L.node("PortaSaida", inst="door", props={"position": v(11 * T + 32, 5 * T), "Size": v(3 * T, 64), "StartsLocked": "true",
       "WallStyle": "true", "Color": col(0.9, 0.75, 0.3, 1)})
L.node("CoberturaSaida", type="ColorRect", props={"z_index": "20", "offset_left": f"{8 * T}.0", "offset_top": "0.0",
       "offset_right": f"{15 * T}.0", "offset_bottom": f"{4 * T}.0", "mouse_filter": "2",
       "color": col(0.03, 0.035, 0.045, 1)})
L.node("Saida", inst="area_trigger", props={"position": v(9 * T, 1 * T), "Size": v(5 * T, 3 * T), "Text": '"4º ANDAR"'})
L.prop("PortaEscadaEntrada", ".", 2 * T + 24, 23 * T, 48, 3 * T, col(0.5, 0.42, 0.3), depth=3 * T, solid=False,
       sprite="porta_escada")
L.prop("PlacaEntrada", ".", 3 * T + 32, 20 * T + 8, 220, 30, col(0, 0, 0, 0), "ESCADA", font=16,
       text_color=col(0.95, 0.9, 0.6), solid=False, z=1)

# ---------- noite: luz de emergência da escada (fraca, piscando) e a chuva lá fora ----------
L.node("Noite", type="CanvasModulate", groups=["night"], props={"color": col(0.11, 0.12, 0.2, 1)})
L.node("NightOverlay", inst="night_overlay")
L.node("Chuva", inst="rain", props={"AlwaysInside": "true", "LightningInterval": v(12, 26)})
STEADY, FLICKER, BROKEN, PULSE = 0, 1, 2, 3
L.node("Luzes", type="Node2D")
for i, (tx, ty, mode) in enumerate([(3.5, 18, FLICKER), (3.5, 10, BROKEN), (8, 6.5, FLICKER), (15, 6.5, BROKEN),
                                    (19.5, 9, FLICKER), (19.5, 16, BROKEN), (14, 18.5, FLICKER), (8, 18.5, BROKEN)]):
    L.node(f"Emergencia{i+1}", "Luzes", inst="flicker_light", props={
        "position": v(tx * T, ty * T), "color": col(1, 0.82, 0.55), "texture_scale": "1.8", "BaseEnergy": "0.8",
        "Behavior": str(mode), "Seed": str(500 + i), "texture": 'ExtResource("light_tubo")'})
L.node("LuzSaida", "Luzes", inst="flicker_light", props={"position": v(11 * T + 32, 3 * T), "color": col(0.2, 1, 0.45),
       "texture_scale": "1.2", "BaseEnergy": "0.8", "Behavior": str(STEADY), "Seed": "520"})

L.node("Player", inst="player", props={"position": v(3 * T + 32, 21 * T + 48), "StartWithAllWeapons": "false"})
L.node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
L.node("MessageScreen", inst="message_screen")

paths = {"Player": "Player", "Hud": "Hud", "Screen": "MessageScreen", "Subida": "Subida", "PortaSaida": "PortaSaida",
         "Saida": "Saida", "CoberturaSaida": "CoberturaSaida", "Desabamento": "Desabamento", "Barricadas": "Barricadas"}
L.write(sys.argv[1], root_node("Escada", "escada", paths))
print("chão", len(L.floor))
