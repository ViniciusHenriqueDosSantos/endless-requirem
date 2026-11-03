# Gera levels/escada.tscn: escada em espiral (vista de cima), do 2º ao 4º andar.
# Cada volta da espiral é um andar. Uso: python3 tools/levelgen/gen_escada.py levels/escada.tscn
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from lib import Level, T, v, col, px, root_node, CONCRETE

L = Level()
LAPS = 2            # voltas (2º -> 3º -> 4º)
W = 3               # largura do corredor da escada (tiles)
STEP = W + 1        # corredor + parede

left, top, right, bottom = 2, 2, 22, 22
L.add_floor(1, 23, 4, 26)              # patamar de chegada (começo); só encosta na subida da esquerda
start_bottom = 23
lap_starts = []                        # onde começa cada volta (para o contador de andar)
for lap in range(LAPS):
    L.add_floor(left, top, left + W - 1, start_bottom)        # sobe pela esquerda
    L.add_floor(left, top, right, top + W - 1)                # vai para a direita em cima
    L.add_floor(right - W + 1, top, right, bottom)            # desce pela direita
    L.add_floor(left + STEP, bottom - W + 1, right, bottom)   # volta para a esquerda embaixo
    lap_starts.append((left + STEP, bottom - W + 1))
    start_bottom = bottom
    left += STEP; top += STEP; right -= STEP; bottom -= STEP
L.add_floor(left, top, left + W - 1, start_bottom)            # última subida até o topo
L.add_floor(left, top, right, bottom)                         # sala do 4º andar (centro)
center = ((left + right) / 2, (top + bottom) / 2)
L.build_walls()

L.res('Script', 'res://levels/Escada.cs', 'escada')
for name, path in [('player', 'player/player'), ('hud', 'hud/hud'), ('message_screen', 'message_screen/message_screen'),
                   ('room', 'room/room'), ('area_trigger', 'area_trigger/area_trigger')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
L.tile_layers()

# sala única: fantasmas surgem sem parar enquanto o jogador sobe
L.node("Subida", inst="room", props={"position": v(2 * T, 2 * T), "Size": v(21 * T, 21 * T), "Waves": "0",
       "SurviveTime": "9999.0", "SurviveSpawnInterval": "1.5", "SurviveMaxAlive": "5", "SpawnAnywhere": "true",
       "RespawnPoint": 'NodePath("Respawn")'}, node_paths=["RespawnPoint"])
L.node("Respawn", "Subida", type="Marker2D", props={"position": v(3 * T - 2 * T, 25 * T - 2 * T)})

# marcas de andar: ao entrar na 2ª volta = 3º andar; na sala do centro = 4º andar
lx, ly = 2 + STEP, 2 + STEP   # canto de cima da 2ª volta (subida da esquerda)
L.node("Andar3", inst="area_trigger", props={"position": v(lx * T, 14 * T), "Size": v(W * T, 2 * T),
       "Color": col(0.3, 1, 0.55, 0.12)})
L.node("Topo", inst="area_trigger", props={"position": v((center[0] - 1) * T, (center[1] - 1) * T),
       "Size": v(3 * T, 3 * T), "Text": '"4º ANDAR"'})

# placas e degraus pintados no chão (decoração)
L.prop("PlacaEscada", ".", 3 * T, 23 * T + 8, 220, 30, col(0, 0, 0, 0), "ESCADA  -  2º ANDAR", font=16,
       text_color=col(0.95, 0.9, 0.6), solid=False, z=1)
for (x, y) in sorted(L.floor):
    if (x + y) % 4 == 0 and y < 23:
        L.node(f"Degrau_{x}_{y}", type="ColorRect", props={"z_index": "-5", "offset_left": f"{x*T}.0",
               "offset_top": f"{y*T + 28}.0", "offset_right": f"{(x+1)*T}.0", "offset_bottom": f"{y*T + 36}.0",
               "mouse_filter": "2", "color": col(0.75, 0.7, 0.6, 0.6)})

L.node("Player", inst="player", props={"position": v(3 * T, 25 * T + 32), "StartWithAllWeapons": "false"})
L.node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
L.node("MessageScreen", inst="message_screen")

paths = {"Player": "Player", "Hud": "Hud", "Screen": "MessageScreen", "Subida": "Subida", "Andar3": "Andar3", "Topo": "Topo"}
L.write(sys.argv[1], root_node("Escada", "escada", paths))
print("chão", len(L.floor), "centro", center, "voltas", lap_starts)
