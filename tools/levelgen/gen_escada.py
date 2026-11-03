# Gera levels/escada.tscn: escada circular vista de cima (fim da Fase 2).
# É uma volta só: o jogador dá voltas no mesmo anel e cada volta completa é um andar
# (o contador na tela mostra o andar e o tempo). Corrida contra o relógio, com fantasmas
# pesados parados nos degraus. A porta de saída só aparece no 4º andar (antes é parede).
# Uso: python3 tools/levelgen/gen_escada.py levels/escada.tscn
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from lib import Level, T, v, col, px, root_node, CONCRETE

L = Level()
W = 3                                   # largura do anel (tiles)
X0, Y0, X1, Y1 = 2, 5, 20, 19           # contorno de fora do anel
for (a, b, c, d) in [(X0, Y0, X1, Y0 + W - 1), (X0, Y1 - W + 1, X1, Y1),
                     (X0, Y0, X0 + W - 1, Y1), (X1 - W + 1, Y0, X1, Y1)]:
    L.add_floor(a, b, c, d)
L.add_floor(2, 20, 4, 22)               # patamar de entrada (embaixo, à esquerda)
L.add_floor(10, 4, 12, 4)               # porta de saída (em cima)
L.add_floor(9, 1, 13, 3)                # patamar do 4º andar
L.build_walls()

L.res('Script', 'res://levels/Escada.cs', 'escada')
for name, path in [('player', 'player/player'), ('hud', 'hud/hud'), ('message_screen', 'message_screen/message_screen'),
                   ('room', 'room/room'), ('area_trigger', 'area_trigger/area_trigger'), ('door', 'door/door')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
L.tile_layers()

# vão da escada (centro): só decoração escura
L.node("Vao", type="ColorRect", props={"z_index": "-5", "offset_left": f"{(X0 + W) * T}.0",
       "offset_top": f"{(Y0 + W) * T}.0", "offset_right": f"{(X1 - W + 1) * T}.0",
       "offset_bottom": f"{(Y1 - W + 1) * T}.0", "mouse_filter": "2", "color": col(0.05, 0.06, 0.07, 1)})
L.prop("PlacaVao", ".", 11 * T + 32, 13 * T, 360, 40, col(0, 0, 0, 0), "vão da escada", font=18,
       text_color=col(0.35, 0.4, 0.38), solid=False, z=1)

# sala única: fantasmas surgem sem parar enquanto o jogador sobe
L.node("Subida", inst="room", props={"position": v(X0 * T, Y0 * T), "Size": v((X1 - X0 + 1) * T, (Y1 - Y0 + 1) * T),
       "Waves": "0", "SurviveTime": "9999.0", "SurviveSpawnInterval": "1.5", "SurviveMaxAlive": "5",
       "SpawnAnywhere": "true", "RespawnPoint": 'NodePath("Respawn")'}, node_paths=["RespawnPoint"])
L.node("Respawn", "Subida", type="Marker2D", props={"position": v(3 * T + 32 - X0 * T, 21 * T + 32 - Y0 * T)})

# marcos da volta, na ordem em que se sobe: esquerda -> cima -> direita -> baixo
L.node("Marcos", type="Node2D")
for i, (x, y, w, h) in enumerate([(X0, 11, W, 2), (10, Y0, 3, W), (X1 - W + 1, 11, W, 2), (10, Y1 - W + 1, 3, W)]):
    L.node(f"Marco{i}", "Marcos", inst="area_trigger", props={"position": v(x * T, y * T), "Size": v(w * T, h * T),
           "Color": col(0.3, 1, 0.55, 0.06)})

# setas pintadas no chão indicando o sentido da subida
for i, (x, y, t) in enumerate([(3, 15, "↑"), (3, 9, "↑"), (6, 6, "→"), (16, 6, "→"), (19, 9, "↓"), (19, 15, "↓"),
                               (16, 18, "←"), (6, 18, "←")]):
    L.prop(f"Seta{i}", ".", x * T + 32, y * T + 48, 64, 64, col(0, 0, 0, 0), t, font=40,
           text_color=col(0.55, 0.5, 0.42, 0.8), solid=False, z=-4)

# fantasmas pesados parados nos degraus (um por lado, na faixa do meio)
L.node("Bloqueios", type="Node2D")
for i, (x, y) in enumerate([(3.5, 8.5), (15.5, 6.5), (19.5, 15.5), (7.5, 18.5)]):
    L.node(f"Bloqueio{i}", "Bloqueios", type="Marker2D", props={"position": v(x * T, y * T + 32)})

# saída do 4º andar: parece parede até chegar lá; o patamar fica coberto
L.node("PortaSaida", inst="door", props={"position": v(11 * T + 32, 5 * T), "Size": v(3 * T, 64), "StartsLocked": "true",
       "WallStyle": "true", "Color": col(0.9, 0.75, 0.3, 1)})
L.node("CoberturaSaida", type="ColorRect", props={"z_index": "20", "offset_left": f"{8 * T}.0", "offset_top": "0.0",
       "offset_right": f"{15 * T}.0", "offset_bottom": f"{4 * T}.0", "mouse_filter": "2",
       "color": col(0.149, 0.161, 0.188, 1)})
L.node("Saida", inst="area_trigger", props={"position": v(9 * T, 1 * T), "Size": v(5 * T, 3 * T), "Text": '"4º ANDAR"'})
L.prop("PlacaEntrada", ".", 3 * T + 32, 20 * T + 8, 220, 30, col(0, 0, 0, 0), "ESCADA", font=16,
       text_color=col(0.95, 0.9, 0.6), solid=False, z=1)

L.node("Player", inst="player", props={"position": v(3 * T + 32, 21 * T + 48), "StartWithAllWeapons": "false"})
L.node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
L.node("MessageScreen", inst="message_screen")

paths = {"Player": "Player", "Hud": "Hud", "Screen": "MessageScreen", "Subida": "Subida", "Marcos": "Marcos",
         "Bloqueios": "Bloqueios", "PortaSaida": "PortaSaida", "Saida": "Saida", "CoberturaSaida": "CoberturaSaida"}
L.write(sys.argv[1], root_node("Escada", "escada", paths))
print("chão", len(L.floor))
