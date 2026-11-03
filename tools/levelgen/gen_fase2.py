# Gera levels/fase_2.tscn (Biblioteca) a partir da planta do João.
# Uso: python3 tools/levelgen/gen_fase2.py levels/fase_2.tscn
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from lib import Level, T, v, col, px, root_node, CONCRETE, WHITE, BEGE, WOOD

L = Level()

# ---------- planta (tiles, retângulos inclusivos) ----------
L.add_floor(24, -8, 29, 46)   # corredor principal (os dois elevadores ficam na parede da esquerda)
L.add_floor(2, 34, 22, 40)    # Hall das Caronas
L.add_floor(23, 36, 23, 38)   # abertura Hall -> corredor (as cadeiras bloqueiam)
L.add_floor(1, 35, 1, 39)     # parada do Uniride (parede da esquerda do Hall: portão trancado)
L.add_floor(20, 18, 23, 25)   # entrada da biblioteca (devolução ao lado da porta)
L.add_floor(2, 9, 18, 31)     # biblioteca
L.add_floor(19, 21, 19, 23)   # porta da biblioteca
# salas de estudo: dois corredores saindo da biblioteca para cima; no fim de cada um,
# uma porta à esquerda e outra à direita levam às salas (3 x 5 tiles, linhas 3..7,
# encostadas na parede de cima da biblioteca).
STUDY_CORRIDORS = [(6, 7), (17, 18)]                   # (x0, x1), linhas 3..8
STUDY_ROOMS = [(2, 4), (9, 11), (13, 15), (20, 22)]    # (x0, x1) de cada sala, linhas 3..7
STUDY_DOORS = [5, 8, 16, 19]                           # porta de 1 tile (linha 5) entre corredor e sala
for (a, b) in STUDY_CORRIDORS:
    L.add_floor(a, 3, b, 8)
for (a, b) in STUDY_ROOMS:
    L.add_floor(a, 3, b, 7)
for x in STUDY_DOORS:
    L.add_floor(x, 5, x, 5)
L.add_floor(31, 17, 35, 22)   # Achados e Perdidos (sala pequena)
L.add_floor(30, 19, 30, 20)   # porta dos Achados
L.add_floor(20, -5, 22, -1)   # patamar da escada (acima das salas de estudo, logo abaixo do elevador quebrado)
L.add_floor(23, -4, 23, -3)   # porta da escada
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
L.res('Script', 'res://objects/decor/Signatures.cs', 'sig')
L.res('Resource', 'res://objects/weapon/cannon.tres', 'cannon')

L.tile_layers()

# ---------- elevador de chegada (na parede da esquerda, embaixo) ----------
L.prop("ElevadorChegada", ".", 24 * T + 24, 47 * T, 48, 3 * T, CONCRETE, "E\nL\nE\nV", depth=3 * T, font=13, tall=True)

# ---------- assinaturas dos alunos nas paredes do corredor ----------
def wall_runs(x, y0, y1):
    run = []
    for y in range(y0, y1 + 2):
        if y <= y1 and (x, y) not in L.floor:
            run.append(y)
        elif run:
            yield run[0], run[-1]
            run = []
n = 0
for x, facing in ((23, "1"), (30, "2")):       # 1 = parede da esquerda, 2 = parede da direita (texto virado para o corredor)
    for a, b in wall_runs(x, -9, 47):
        if b - a < 1: continue
        n += 1
        L.node(f"Assinaturas{n}", type="Node2D", props={"z_index": "1", "position": v(x * T, a * T),
               "script": 'ExtResource("sig")', "Size": v(T, (b - a + 1) * T), "Seed": str(n * 7919), "Facing": facing})
L.node("AssinaturasTopo", type="Node2D", props={"z_index": "1", "position": v(24 * T, -9 * T),
       "script": 'ExtResource("sig")', "Size": v(6 * T, T), "Seed": "4242", "Facing": "0"})

# ---------- Hall das Caronas ----------
hall = (2 * T, 34 * T)
L.node("Hall", inst="room", props={"position": v(*hall), "Size": v(22 * T, 7 * T), "Waves": "3", "EnemiesPerWave": "6",
       "FastChance": "0", "EnemyHealthScale": "0.6", "EnemySpeedScale": "1.1",  "SpawnAnywhere": "true", "FinalWaveTank": "true", "StartOnEnter": "false",
       "FinalWaveMessage": '"O ZELADOR CHEGOU!"', "RespawnPoint": 'NodePath("Respawn")'}, node_paths=["RespawnPoint"])
L.node("Barreira", "Hall", inst="door", props={"position": v(23 * T + 32 - hall[0], 39 * T - hall[1]),
       "Size": v(64, 3 * T), "ChairStyle": "true"})
L.node("Respawn", "Hall", type="Marker2D", props={"position": v(26 * T - hall[0], 37 * T + 32 - hall[1])})
L.node("CentroHall", type="Marker2D", props={"position": v(12 * T, 37 * T + 32)})
# cadeiras de espera encostadas na parede de cima; as últimas (perto da saída) se arrastam para bloquear
L.node("CadeirasDaSaida", "Hall", type="Node2D", props={"y_sort_enabled": "true"})
for i, x in enumerate(range(4, 22, 2)):
    parent = "Hall/CadeirasDaSaida" if x >= 16 else "Hall"
    base = (0, 0) if parent == "Hall" else (0, 0)
    L.prop(f"Cadeira{i+1}", parent, x * T + 32 - hall[0], 35 * T - hall[1], 44, 52, col(0.2, 0.2, 0.22), depth=28)
L.prop("PlacaCaronas", "Hall", 12 * T - hall[0], 34 * T + 4 - hall[1], 300, 32, col(0, 0, 0, 0), "PONTO DE CARONA",
       font=18, text_color=col(0.9, 0.9, 0.85), solid=False, z=1)
# parede da esquerda: parada do Uniride, um portão trancado (só dá a mensagem)
L.node("PortaUniride", inst="door", props={"position": v(T + 32, 40 * T), "Size": v(64, 5 * T), "StartsLocked": "true",
       "MessageWhenLocked": "true", "Color": col(0.35, 0.55, 0.95, 1),
       "LockedMessage": '"Uniride parou de funcionar faz tempo... continue a procurar seu diploma."'})
L.prop("PlacaUniride", ".", 2 * T + 48, 35 * T + 8, 120, 26, col(0, 0, 0, 0), "UNIRIDE", font=14,
       text_color=col(0.6, 0.75, 1), solid=False, z=1)
L.node("Chave", inst="pickup", props={"position": v(12 * T, 37 * T), "ItemId": '"chave_achados"',
       "DisplayName": '"Chave dos Achados"', "Color": col(1, 0.85, 0.2, 1), "StartsHidden": "true"})

# ---------- Achados e Perdidos ----------
ach = (31 * T, 17 * T)
L.node("Achados", inst="room", props={"position": v(*ach), "Size": v(5 * T, 6 * T), "Waves": "0", "EnemyHealthScale": "0.6", "EnemySpeedScale": "1.1"})
L.node("PortaAchados", inst="door", props={"position": v(30 * T + 32, 21 * T), "Size": v(64, 2 * T),
       "StartsLocked": "true", "RequiredItem": '"chave_achados"', "Color": col(0.9, 0.6, 0.3, 1),
       "LockedMessage": '"ACHADOS E PERDIDOS - trancado. \\"Chave com o zelador (Hall das Caronas).\\""'})
L.prop("PlacaAchados", ".", 30 * T + 32, 19 * T - 4, 200, 30, col(0, 0, 0, 0), "ACHADOS E PERDIDOS", font=13,
       text_color=col(0.95, 0.9, 0.8), solid=False, z=1)
L.node("Caixas", "Achados", type="Node2D", props={"y_sort_enabled": "true"})
# caixas enfileiradas na parede de baixo e na da direita (o conteúdo é decidido quando quebram: Fase2.cs)
for i, (tx, ty) in enumerate([(x, 22) for x in range(31, 36)] + [(35, y) for y in range(19, 22)]):
    L.node(f"Caixa{i+1}", "Achados/Caixas", inst="paper_pile",
           props={"position": v(tx * T + 32 - ach[0], ty * T + 48 - ach[1]), "Health": "3", "Box": "true"})
# decoração: lousa na parede de cima, armário no canto, mesa e cadeira no meio
L.prop("Lousa", "Achados", 33 * T + 32 - ach[0], 17 * T + 4 - ach[1], 2 * T + 48, 52, col(0.12, 0.3, 0.22),
       "achados:\n1 guarda-chuva...", font=10, text_color=col(0.85, 0.9, 0.85), solid=False)
L.prop("Armario", "Achados", 35 * T + 32 - ach[0], 18 * T + 40 - ach[1], 60, 130, WOOD, "", depth=40, tall=True)
L.prop("MesaAchados", "Achados", 33 * T - ach[0], 20 * T + 16 - ach[1], 110, 52, WOOD, depth=36)
L.prop("CadeiraAchados", "Achados", 33 * T - ach[0], 21 * T + 8 - ach[1], 36, 46, col(0.2, 0.2, 0.22), depth=22)
L.node("Livro", inst="pickup", props={"position": v(32 * T, 20 * T), "ItemId": '"livro"',
       "DisplayName": '"Livro: Cálculo I"', "Color": col(0.85, 0.3, 0.3, 1), "StartsHidden": "true"})

# ---------- entrada da biblioteca ----------
L.prop("CaixaDevolucao", ".", 20 * T + 32, 21 * T, 64, 80, WOOD, "DEVOL-\nUÇÃO", depth=48, font=11,
       text_color=col(0.95, 0.9, 0.8), blocks_ghosts=True)
L.node("Devolucao", inst="item_slot", props={"position": v(20 * T + 32, 21 * T), "Size": v(180, 150),
       "RequiredItem": '"livro"', "MissingMessage": '"Caixa de devolução. Falta o livro \\"Cálculo I\\"."'})

# ---------- biblioteca ----------
bib = (2 * T, 2 * T)
L.node("Biblioteca", inst="room", props={"position": v(*bib), "Size": v(17 * T, 30 * T), "Waves": "0", "HasBoss": "true",
       "FastChance": "0", "EnemyHealthScale": "0.6", "EnemySpeedScale": "1.1",  "SpawnAnywhere": "true", "RespawnPoint": 'NodePath("Respawn")'}, node_paths=["RespawnPoint"])
L.node("PortaBiblioteca", "Biblioteca", inst="door", props={"position": v(19 * T + 32 - bib[0], 24 * T - bib[1]),
       "Size": v(64, 3 * T), "StartsLocked": "true", "MessageWhenLocked": "true", "Color": col(0.6, 0.5, 0.9, 1),
       "LockedMessage": '"BIBLIOTECA FECHADA. Devolução pendente: \\"Cálculo I\\" (atrasado desde 2041). ...Shhh! Sem o livro, sem entrada."'})
L.node("Respawn", "Biblioteca", type="Marker2D", props={"position": v(21 * T - bib[0], 22 * T + 32 - bib[1])})
# estantes: altas (bloqueiam tiros e a visão da Bibliotecária) e seguram fantasmas
for i, ty in enumerate([14, 17, 20, 23]):
    L.prop(f"Estante{i+1}", "Biblioteca", 7 * T - bib[0], ty * T - bib[1], 8 * T, 128, WOOD, "", depth=64,
           tall=True, blocks_ghosts=True)
# piso xadrez (decoração no chão)
for yy in range(24, 31):          # piso xadrez 7 x 7
    for xx in range(3, 10):
        if (xx + yy) % 2 == 0:
            L.node(f"Xadrez_{xx}_{yy}", type="ColorRect", props={"z_index": "-5", "offset_left": f"{xx*T}.0",
                   "offset_top": f"{yy*T}.0", "offset_right": f"{(xx+1)*T}.0", "offset_bottom": f"{(yy+1)*T}.0",
                   "mouse_filter": "2", "color": col(0.2, 0.2, 0.22, 1)})
# mesas de estudo (baixas)
L.prop("MesaEstudo1", "Biblioteca", 14 * T - bib[0], 13 * T - bib[1], 128, 60, WHITE, depth=40)
L.prop("MesaEstudo2", "Biblioteca", 14 * T - bib[0], 27 * T - bib[1], 128, 60, WHITE, depth=40)
# luminárias (uma por sala de estudo)
L.node("Luminarias", "Biblioteca", type="Node2D", props={"y_sort_enabled": "true"})
for i, (a, b) in enumerate(STUDY_ROOMS):
    L.node(f"Luminaria{i+1}", "Biblioteca/Luminarias", inst="lamp",
           props={"position": v((a + b + 1) * T / 2 - bib[0], 4 * T + 48 - bib[1])})
# mesas de estudo dentro de cada sala + soleira de madeira nas portas (para parecer porta)
for i, (a, b) in enumerate(STUDY_ROOMS):
    L.prop(f"MesaSala{i+1}", "Biblioteca", (a + b + 1) * T / 2 - bib[0], 7 * T + 8 - bib[1], 150, 56, WHITE, depth=36)
for i, x in enumerate(STUDY_DOORS):
    L.node(f"PortaSala{i+1}", type="ColorRect", props={"z_index": "-4", "offset_left": f"{x * T + 8}.0",
           "offset_top": f"{5 * T}.0", "offset_right": f"{x * T + 56}.0", "offset_bottom": f"{6 * T}.0",
           "mouse_filter": "2", "color": col(0.55, 0.38, 0.22, 1)})
# patrulha da Bibliotecária
L.node("Patrulha", "Biblioteca", type="Node2D")
for i, (tx, ty) in enumerate([(9, 11), (17, 10), (17, 19), (12, 22), (16, 29), (10, 30), (3, 27), (12, 16)]):
    x, y = px(tx, ty)
    L.node(f"Ponto{i+1}", "Biblioteca/Patrulha", type="Marker2D", props={"position": v(x - bib[0], y - bib[1])})
L.node("Bibliotecario", "Biblioteca", inst="librarian", props={"position": v(14 * T - bib[0], 20 * T - bib[1]),
       "Config": 'ExtResource("bib")', "Patrol": 'NodePath("../Patrulha")', "Lamps": 'NodePath("../Luminarias")'},
       node_paths=["Patrol", "Lamps"])
L.node("Canhao", inst="pickup", props={"position": v(14 * T, 21 * T), "ItemId": '"canhao"', "DisplayName": '"Canhão"',
       "Color": col(0.3, 0.9, 1, 1), "WeaponReward": 'ExtResource("cannon")', "StartsHidden": "true"})
L.node("ChaveEscada", inst="pickup", props={"position": v(15 * T, 21 * T), "ItemId": '"chave_escada"',
       "DisplayName": '"Chave da escada"', "Color": col(1, 0.85, 0.2, 1), "StartsHidden": "true"})
L.prop("PlacaBiblioteca", ".", 19 * T + 32, 20 * T + 8, 180, 30, col(0, 0, 0, 0), "BIBLIOTECA", font=16,
       text_color=col(0.85, 0.8, 1), solid=False, z=1)

# ---------- elevador quebrado + escada (parede da esquerda, em cima; a escada logo abaixo) ----------
L.prop("ElevadorQuebrado", ".", 24 * T + 24, -5 * T, 48, 3 * T, CONCRETE, "E\nL\nE\nV", depth=3 * T, font=13,
       tall=True, inner=col(0.35, 0.2, 0.2))
L.node("AvisoElevador", inst="item_slot", props={"position": v(25 * T, -6 * T), "Size": v(200, 220),
       "RequiredItem": '"nunca"', "MissingMessage": '"ELEVADOR - fora de serviço desde 2049. Use a escada aqui do lado."'})
L.node("PortaEscada", inst="door", props={"position": v(23 * T + 32, -2 * T), "Size": v(64, 2 * T), "StartsLocked": "true",
       "RequiredItem": '"chave_escada"', "Color": col(0.9, 0.75, 0.3, 1),
       "LockedMessage": '"ESCADA - trancada. A chave está com a Bibliotecária."', "UnlockMessage": '"A porta da escada abriu."'})
L.prop("PlacaEscada", ".", 25 * T + 32, -4 * T + 4, 140, 30, col(0, 0, 0, 0), "ESCADA", font=16,
       text_color=col(0.95, 0.9, 0.6), solid=False, z=1)
L.node("Escada", inst="area_trigger", props={"position": v(20 * T, -5 * T), "Size": v(2 * T, 5 * T), "Text": '"ESCADA"'})

# ---------- jogador / interface ----------
L.node("Player", inst="player", props={"position": v(26 * T + 32, 45 * T + 32), "StartWithAllWeapons": "false"})
L.node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
L.node("MessageScreen", inst="message_screen")

paths = {"Player": "Player", "Hud": "Hud", "Screen": "MessageScreen", "PortaBiblioteca": "Biblioteca/PortaBiblioteca",
         "Devolucao": "Devolucao", "PortaAchados": "PortaAchados", "Hall": "Hall", "Chave": "Chave", "Achados": "Achados",
         "Caixas": "Achados/Caixas", "Livro": "Livro", "Biblioteca": "Biblioteca",
         "Bibliotecario": "Biblioteca/Bibliotecario", "Canhao": "Canhao", "ChaveEscada": "ChaveEscada",
         "PortaEscada": "PortaEscada", "Escada": "Escada", "CentroHall": "CentroHall",
         "CadeirasDaSaida": "Hall/CadeirasDaSaida"}
L.write(sys.argv[1], root_node("Fase2", "fase2", paths))
print("chão", len(L.floor), "paredes dentro", len(L.inner_walls), "fora", len(L.outer_walls))
