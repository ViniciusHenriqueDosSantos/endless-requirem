# Gera levels/fase_2.tscn (Biblioteca) a partir da planta do João.
# Uso: python3 tools/levelgen/gen_fase2.py levels/fase_2.tscn
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from lib import Level, T, v, col, px, root_node, CONCRETE, WHITE, BEGE, WOOD

L = Level()

# ---------- planta (tiles, retângulos inclusivos) ----------
L.add_floor(24, 6, 29, 44)    # corredor principal
L.add_floor(19, 41, 23, 44)   # elevador de chegada (começo)
L.add_floor(2, 32, 22, 38)    # Hall das Caronas
L.add_floor(23, 34, 23, 36)   # abertura Hall -> corredor (barreira de névoa)
L.add_floor(19, 18, 23, 25)   # entrada da biblioteca (mesas + devolução)
L.add_floor(2, 9, 17, 29)     # biblioteca
L.add_floor(18, 21, 18, 23)   # porta da biblioteca
for x0 in (2, 6, 10, 14):     # 4 salas de estudo + passagens
    L.add_floor(x0, 4, x0 + 2, 7)
    L.add_floor(x0 + 1, 8, x0 + 1, 8)
L.add_floor(31, 16, 37, 23)   # Achados e Perdidos
L.add_floor(30, 19, 30, 20)   # porta dos Achados
L.add_floor(19, 6, 23, 9)     # elevador quebrado (topo)
L.add_floor(26, 5, 27, 5)     # porta da escada
L.add_floor(25, 2, 28, 4)     # patamar da escada
L.build_walls()

# ---------- recursos ----------
L.res('Script', 'res://levels/Fase2.cs', 'fase2')
for name, path in [('player', 'player/player'), ('hud', 'hud/hud'), ('message_screen', 'message_screen/message_screen'),
                   ('room', 'room/room'), ('door', 'door/door'), ('pickup', 'pickup/pickup'),
                   ('item_slot', 'item_slot/item_slot'), ('area_trigger', 'area_trigger/area_trigger'),
                   ('paper_pile', 'paper_pile/paper_pile'), ('librarian', 'librarian/librarian'),
                   ('lamp', 'librarian/shield_lamp')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
L.res('Resource', 'res://objects/librarian/bibliotecario.tres', 'bib')
L.res('Resource', 'res://objects/weapon/cannon.tres', 'cannon')

L.tile_layers()

# ---------- elevador de chegada ----------
L.prop("ElevadorChegada", ".", 21 * T + 32, 42 * T, 3 * T, 96, CONCRETE, "ELEVADOR", depth=48, font=14, tall=True)

# ---------- corredor ----------
L.prop("Bebedouro", ".", 29 * T + 20, 30 * T, 40, 72, col(0.7, 0.72, 0.75), depth=32)
L.prop("QuadroAvisos", ".", 24 * T + 24, 12 * T, 40, 96, col(0.55, 0.45, 0.3), "avisos", depth=24, font=10, solid=False)

# ---------- Hall das Caronas ----------
hall = (2 * T, 32 * T)
L.node("Hall", inst="room", props={"position": v(*hall), "Size": v(22 * T, 7 * T), "Waves": "3", "EnemiesPerWave": "4",
       "FastChance": "40", "SpawnAnywhere": "true", "FinalWaveTank": "true",
       "FinalWaveMessage": '"O MOTORISTA DA VAN CHEGOU!"', "RespawnPoint": 'NodePath("Respawn")'}, node_paths=["RespawnPoint"])
L.node("Barreira", "Hall", inst="door", props={"position": v(23 * T + 32 - hall[0], 37 * T - hall[1]),
       "Size": v(64, 3 * T), "Color": col(0.3, 1, 0.55, 1)})
L.node("Respawn", "Hall", type="Marker2D", props={"position": v(26 * T - hall[0], 35 * T + 32 - hall[1])})
L.node("CentroHall", type="Marker2D", props={"position": v(12 * T, 35 * T + 32)})
for i, x in enumerate(range(4, 22, 2)):   # cadeiras de espera encostadas na parede de cima
    L.prop(f"Cadeira{i+1}", "Hall", x * T + 32 - hall[0], 33 * T - hall[1], 44, 52, col(0.2, 0.2, 0.22), depth=28)
L.prop("PlacaCaronas", "Hall", 12 * T - hall[0], 32 * T + 4 - hall[1], 300, 32, col(0, 0, 0, 0), "PONTO DE CARONA",
       font=18, text_color=col(0.9, 0.9, 0.85), solid=False, z=1)
L.node("Chave", inst="pickup", props={"position": v(12 * T, 35 * T), "ItemId": '"chave_achados"',
       "DisplayName": '"Chave dos Achados"', "Color": col(1, 0.85, 0.2, 1), "StartsHidden": "true"})

# ---------- Achados e Perdidos ----------
ach = (31 * T, 16 * T)
L.node("Achados", inst="room", props={"position": v(*ach), "Size": v(7 * T, 8 * T), "Waves": "0"})
L.node("PortaAchados", inst="door", props={"position": v(30 * T + 32, 21 * T), "Size": v(64, 2 * T),
       "StartsLocked": "true", "RequiredItem": '"chave_achados"', "Color": col(0.9, 0.6, 0.3, 1),
       "LockedMessage": '"ACHADOS E PERDIDOS - trancado. \\"Chave com o motorista da van (Hall das Caronas).\\""'})
L.prop("PlacaAchados", ".", 30 * T + 32, 19 * T - 4, 200, 30, col(0, 0, 0, 0), "ACHADOS E PERDIDOS", font=13,
       text_color=col(0.95, 0.9, 0.8), solid=False, z=1)
L.node("Caixas", "Achados", type="Node2D", props={"y_sort_enabled": "true"})
for i, (tx, ty) in enumerate([(32, 17), (34, 17), (36, 17), (33, 19), (36, 20), (32, 22), (34, 22), (37, 23)]):
    L.node(f"Caixa{i+1}", "Achados/Caixas", inst="paper_pile",
           props={"position": v(tx * T + 32 - ach[0], ty * T + 48 - ach[1]), "Health": "3", "Box": "true"})
L.node("Livro", inst="pickup", props={"position": v(34 * T, 20 * T), "ItemId": '"livro"',
       "DisplayName": '"Livro: Cálculo I"', "Color": col(0.85, 0.3, 0.3, 1), "StartsHidden": "true"})

# ---------- entrada da biblioteca ----------
L.prop("MesaEntrada1", ".", 21 * T, 21 * T, 96, 60, WHITE, depth=40)
L.prop("MesaEntrada2", ".", 22 * T, 24 * T + 32, 96, 60, WHITE, depth=40)
L.prop("CaixaDevolucao", ".", 20 * T, 19 * T, 96, 80, WOOD, "DEVOLUÇÃO", depth=48, font=12,
       text_color=col(0.95, 0.9, 0.8), blocks_ghosts=True)
L.node("Devolucao", inst="item_slot", props={"position": v(20 * T, 19 * T), "Size": v(200, 150),
       "RequiredItem": '"livro"', "MissingMessage": '"Caixa de devolução. Falta o livro \\"Cálculo I\\"."'})

# ---------- biblioteca ----------
bib = (2 * T, 4 * T)
L.node("Biblioteca", inst="room", props={"position": v(*bib), "Size": v(16 * T, 26 * T), "Waves": "0", "HasBoss": "true",
       "FastChance": "60", "SpawnAnywhere": "true", "RespawnPoint": 'NodePath("Respawn")'}, node_paths=["RespawnPoint"])
L.node("PortaBiblioteca", "Biblioteca", inst="door", props={"position": v(18 * T + 32 - bib[0], 24 * T - bib[1]),
       "Size": v(64, 3 * T), "StartsLocked": "true", "MessageWhenLocked": "true", "Color": col(0.6, 0.5, 0.9, 1),
       "LockedMessage": '"BIBLIOTECA FECHADA. Devolução pendente: \\"Cálculo I\\" (atrasado desde 2041). ...Shhh! Sem o livro, sem entrada."'})
L.node("Respawn", "Biblioteca", type="Marker2D", props={"position": v(21 * T - bib[0], 22 * T + 32 - bib[1])})
# estantes: altas (bloqueiam tiros e a visão do Bibliotecário) e seguram fantasmas
for i, ty in enumerate([14, 17, 20, 23]):
    L.prop(f"Estante{i+1}", "Biblioteca", 7 * T - bib[0], ty * T - bib[1], 8 * T, 128, WOOD, "", depth=64,
           tall=True, blocks_ghosts=True)
# piso xadrez (decoração no chão)
for yy in range(25, 29):
    for xx in range(3, 10):
        if (xx + yy) % 2 == 0:
            L.node(f"Xadrez_{xx}_{yy}", type="ColorRect", props={"z_index": "-5", "offset_left": f"{xx*T}.0",
                   "offset_top": f"{yy*T}.0", "offset_right": f"{(xx+1)*T}.0", "offset_bottom": f"{(yy+1)*T}.0",
                   "mouse_filter": "2", "color": col(0.2, 0.2, 0.22, 1)})
# mesas de estudo (baixas)
L.prop("MesaEstudo1", "Biblioteca", 14 * T - bib[0], 13 * T - bib[1], 128, 60, WHITE, depth=40)
L.prop("MesaEstudo2", "Biblioteca", 14 * T - bib[0], 26 * T - bib[1], 128, 60, WHITE, depth=40)
# luminárias (uma por sala de estudo)
L.node("Luminarias", "Biblioteca", type="Node2D", props={"y_sort_enabled": "true"})
for i, x0 in enumerate((2, 6, 10, 14)):
    L.node(f"Luminaria{i+1}", "Biblioteca/Luminarias", inst="lamp",
           props={"position": v((x0 + 1) * T + 32 - bib[0], 6 * T + 48 - bib[1])})
# patrulha do Bibliotecário
L.node("Patrulha", "Biblioteca", type="Node2D")
for i, (tx, ty) in enumerate([(12, 11), (16, 11), (16, 19), (12, 22), (16, 27), (10, 27), (3, 26), (12, 16)]):
    x, y = px(tx, ty)
    L.node(f"Ponto{i+1}", "Biblioteca/Patrulha", type="Marker2D", props={"position": v(x - bib[0], y - bib[1])})
L.node("Bibliotecario", "Biblioteca", inst="librarian", props={"position": v(14 * T - bib[0], 20 * T - bib[1]),
       "Config": 'ExtResource("bib")', "Patrol": 'NodePath("../Patrulha")', "Lamps": 'NodePath("../Luminarias")'},
       node_paths=["Patrol", "Lamps"])
L.node("Canhao", inst="pickup", props={"position": v(14 * T, 21 * T), "ItemId": '"canhao"', "DisplayName": '"Canhão"',
       "Color": col(0.3, 0.9, 1, 1), "WeaponReward": 'ExtResource("cannon")', "StartsHidden": "true"})
L.node("ChaveEscada", inst="pickup", props={"position": v(15 * T, 21 * T), "ItemId": '"chave_escada"',
       "DisplayName": '"Chave da escada"', "Color": col(1, 0.85, 0.2, 1), "StartsHidden": "true"})
L.prop("PlacaBiblioteca", ".", 18 * T + 32, 20 * T + 8, 180, 30, col(0, 0, 0, 0), "BIBLIOTECA", font=16,
       text_color=col(0.85, 0.8, 1), solid=False, z=1)

# ---------- elevador quebrado + escada ----------
L.prop("ElevadorQuebrado", ".", 21 * T, 7 * T, 3 * T, 120, CONCRETE, "ELEVADOR\nfora de serviço", depth=56, font=13,
       tall=True)
L.node("AvisoElevador", inst="item_slot", props={"position": v(21 * T, 7 * T), "Size": v(240, 160),
       "RequiredItem": '"nunca"', "MissingMessage": '"Fora de serviço desde 2049. Use a escada ao lado."'})
L.node("PortaEscada", inst="door", props={"position": v(27 * T, 6 * T), "Size": v(2 * T, 64), "StartsLocked": "true",
       "RequiredItem": '"chave_escada"', "Color": col(0.9, 0.75, 0.3, 1),
       "LockedMessage": '"ESCADA - trancada. A chave está com o Bibliotecário."', "UnlockMessage": '"A porta da escada abriu."'})
L.prop("PlacaEscada", ".", 27 * T, 5 * T - 8, 160, 30, col(0, 0, 0, 0), "ESCADA", font=16,
       text_color=col(0.95, 0.9, 0.6), solid=False, z=1)
L.node("Escada", inst="area_trigger", props={"position": v(25 * T, 2 * T), "Size": v(4 * T, 3 * T), "Text": '"ESCADA"'})

# ---------- jogador / interface ----------
L.node("Player", inst="player", props={"position": v(21 * T + 32, 43 * T + 32), "StartWithAllWeapons": "false"})
L.node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
L.node("MessageScreen", inst="message_screen")

paths = {"Player": "Player", "Hud": "Hud", "Screen": "MessageScreen", "PortaBiblioteca": "Biblioteca/PortaBiblioteca",
         "Devolucao": "Devolucao", "PortaAchados": "PortaAchados", "Hall": "Hall", "Chave": "Chave", "Achados": "Achados",
         "Caixas": "Achados/Caixas", "Livro": "Livro", "Biblioteca": "Biblioteca",
         "Bibliotecario": "Biblioteca/Bibliotecario", "Canhao": "Canhao", "ChaveEscada": "ChaveEscada",
         "PortaEscada": "PortaEscada", "Escada": "Escada", "CentroHall": "CentroHall"}
L.write(sys.argv[1], root_node("Fase2", "fase2", paths))
print("chão", len(L.floor), "paredes dentro", len(L.inner_walls), "fora", len(L.outer_walls))
