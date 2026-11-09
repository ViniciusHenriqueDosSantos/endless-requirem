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
L.add_floor(-2, 9, 18, 31)    # biblioteca (as 4 salas de estudo cabem em cima dela)
L.add_floor(19, 21, 19, 23)   # porta da biblioteca
# salas de estudo: dois corredores saindo da biblioteca para cima; no fim de cada um,
# uma porta à esquerda e outra à direita levam às salas (3 x 5 tiles, linhas 3..7,
# encostadas na parede de cima da biblioteca).
STUDY_CORRIDORS = [(2, 3), (13, 14)]                   # (x0, x1), linhas 3..8
STUDY_ROOMS = [(-2, 0), (5, 7), (9, 11), (16, 18)]      # (x0, x1) de cada sala, linhas 3..7
STUDY_DOORS = [1, 4, 12, 15]                           # porta de 1 tile (linha 5) entre corredor e sala
for (a, b) in STUDY_CORRIDORS:
    L.add_floor(a, 3, b, 8)
for (a, b) in STUDY_ROOMS:
    L.add_floor(a, 3, b, 7)
for x in STUDY_DOORS:
    L.add_floor(x, 5, x, 5)
L.add_floor(31, 17, 35, 22)   # Achados e Perdidos (sala pequena)
L.add_floor(30, 19, 30, 20)   # porta dos Achados
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
for name, path in [('night_overlay', 'night/night_overlay'), ('flicker_light', 'night/flicker_light'),
                   ('rain', 'weather/rain'), ('item_inspect', 'inspect/item_inspect')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
L.res('Texture2D', 'res://sprites/lights/tubo.png', 'light_tubo')

# ---------- arte: material do piso de cada célula (fotos do tour: biblioteca e corredor da frente dela) ----------
from tiling import h
XADREZ = (0, 6, 24, 30)   # tapete do xadrez gigante (x0, x1, y0, y1)


def material(x, y):
    if 31 <= x <= 35 and 17 <= y <= 22: return "ceramica"                   # Achados e Perdidos (salinha antiga)
    if (-2 <= x <= 18 and 3 <= y <= 31):                                     # biblioteca e salas de estudo: vinílico bege
        if XADREZ[0] <= x <= XADREZ[1] and XADREZ[2] <= y <= XADREZ[3]:
            return "xadrez_b" if (x + y) % 2 == 0 else "xadrez_a"
        return "vinil"
    return "granito"                                                         # corredor, hall e entradas: granito branco


floor_mat = {c: material(*c) for c in L.floor}
MURAL = {x: f"mural_b{(x + 2) % 4}" for x in range(-2, 19)}


def deco_rule(c, style, below):
    """Frente das paredes: janelões na chuva no fundo do corredor e nas salas de estudo, o mural colorido na parede de
    cima da biblioteca, o quadro verde BIBLIOTECA na entrada e as mãos pintadas dos formandos no hall e no corredor."""
    x, y = c
    r = h("deco2", x, y)
    if style != "interno": return None
    if y in (-9, 2): return "janelao_quebrado" if r < 0.3 else "janelao"
    if y == 8 and below == "vinil": return MURAL.get(x)
    if y == 17 and 20 <= x <= 23: return "quadro_bib" if x == 21 else "maos1"
    if below == "granito":
        if r < 0.55: return f"maos{int(r * 100) % 3}"
        if r < 0.62: return "persiana"
        if r < 0.67: return "saida"
        if r < 0.72: return "extintor"
        if r < 0.80: return "rachada"
        if r < 0.86: return "pichacao1"
        return "mofo"
    if r < 0.2: return "rachada"
    if r < 0.4: return "mofo"
    return None


DECALS = [
    (lambda m: m.startswith("vinil") or m.startswith("xadrez"),
     [("livros", 3, 0.05), ("papel", 2, 0.04), ("rachadura", 3, 0.02), ("mancha", 2, 0.015), ("forro", 1, 0.01),
      ("poca", 2, 0.015), ("vidro", 1, 0.008)]),
    (lambda m: m == "granito",
     [("papel", 2, 0.05), ("rachadura", 3, 0.05), ("mancha", 2, 0.03), ("poca", 2, 0.035), ("forro", 1, 0.012),
      ("folhas", 2, 0.01)]),
    (lambda m: m == "ceramica", [("papel", 2, 0.08), ("rachadura", 3, 0.04), ("mancha", 2, 0.04)]),
]
L.tiled_layers("fase1", floor_mat, deco_rule, decal_rules=DECALS,
               background=lambda x, y: f"laje{int(h('l', x, y) * 3)}")

# ---------- elevador de chegada (na parede da esquerda, embaixo) ----------
L.prop("ElevadorChegada", ".", 24 * T + 24, 47 * T, 48, 3 * T, CONCRETE, depth=3 * T, tall=True, sprite="elevador")

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
    L.prop(f"Cadeira{i+1}", parent, x * T + 32 - hall[0], 35 * T - hall[1], 44, 52, col(0.2, 0.2, 0.22), depth=28,
           sprite="cadeira_espera")
L.prop("PlacaCaronas", "Hall", 12 * T - hall[0], 34 * T + 4 - hall[1], 300, 32, col(0, 0, 0, 0), "PONTO DE CARONA",
       font=18, text_color=col(0.9, 0.9, 0.85), solid=False, z=1)
# parede da esquerda: parada do Uniride, um portão trancado (só dá a mensagem)
L.node("PortaUniride", inst="door", props={"position": v(T + 32, 40 * T), "Size": v(64, 5 * T), "StartsLocked": "true",
       "MessageWhenLocked": "true", "Color": col(0.35, 0.55, 0.95, 1),
       "LockedMessage": '"Uniride parou de funcionar faz tempo... continue a procurar seu diploma."'})
L.prop("PlacaUniride", ".", 2 * T + 48, 35 * T + 8, 120, 26, col(0, 0, 0, 0), "UNIRIDE", font=14,
       text_color=col(0.6, 0.75, 1), solid=False, z=1)
L.node("Chave", inst="pickup", props={"position": v(12 * T, 37 * T), "ItemId": '"chave_achados"',
       "DisplayName": '"Chave dos Achados"', "Color": col(1, 0.85, 0.2, 1), "StartsHidden": "true",
       "Texture": L.tex("chave_achados", "items"), "InspectTexture": L.tex("chave_achados", "items/inspecao"),
       "InspectTitle": '"CHAVE DOS ACHADOS"',
       "InspectText": '"Estava no chaveiro do zelador. Etiqueta amarela: A/P, Achados e Perdidos. A sala fica do outro lado do corredor."'})

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
       solid=False, sprite="lousa")
L.prop("Armario", "Achados", 35 * T + 32 - ach[0], 18 * T + 40 - ach[1], 60, 130, WOOD, "", depth=40, tall=True,
       sprite="armario_madeira")
L.prop("MesaAchados", "Achados", 33 * T - ach[0], 20 * T + 16 - ach[1], 110, 52, WOOD, depth=36, sprite="mesa_achados")
L.prop("CadeiraAchados", "Achados", 33 * T - ach[0], 21 * T + 8 - ach[1], 36, 46, col(0.2, 0.2, 0.22), depth=22,
       sprite="cadeira_espera")
L.node("Livro", inst="pickup", props={"position": v(32 * T, 20 * T), "ItemId": '"livro"',
       "DisplayName": '"Livro: Cálculo I"', "Color": col(0.85, 0.3, 0.3, 1), "StartsHidden": "true",
       "Texture": L.tex("livro", "items"), "InspectTexture": L.tex("livro", "items/inspecao"), "InspectTitle": '"CÁLCULO I"',
       "InspectText": '"Etiqueta da biblioteca do ICT, 515.1. O carimbo de devolução diz 2041. Vinte anos de atraso: alguém lá dentro ainda está esperando."'})

# ---------- entrada da biblioteca ----------
L.prop("CaixaDevolucao", ".", 20 * T + 32, 21 * T, 64, 80, WOOD, depth=48, blocks_ghosts=True, sprite="caixa_devolucao")
L.node("Devolucao", inst="item_slot", props={"position": v(20 * T + 32, 21 * T), "Size": v(180, 150),
       "RequiredItem": '"livro"', "MissingMessage": '"Caixa de devolução. Falta o livro \\"Cálculo I\\"."'})

# ---------- biblioteca ----------
bib = (-2 * T, 2 * T)
L.node("Biblioteca", inst="room", props={"position": v(*bib), "Size": v(21 * T, 30 * T), "Waves": "0", "HasBoss": "true",
       "FastChance": "0", "EnemyHealthScale": "0.6", "EnemySpeedScale": "1.1",  "SpawnAnywhere": "true", "RespawnPoint": 'NodePath("Respawn")'}, node_paths=["RespawnPoint"])
L.node("PortaBiblioteca", "Biblioteca", inst="door", props={"position": v(19 * T + 32 - bib[0], 24 * T - bib[1]),
       "Size": v(64, 3 * T), "StartsLocked": "true", "MessageWhenLocked": "true", "Color": col(0.6, 0.5, 0.9, 1),
       "LockedMessage": '"BIBLIOTECA FECHADA. Devolução pendente: \\"Cálculo I\\" (atrasado desde 2041). ...Shhh! Sem o livro, sem entrada."'})
L.node("Respawn", "Biblioteca", type="Marker2D", props={"position": v(21 * T - bib[0], 22 * T + 32 - bib[1])})
# estantes: altas (bloqueiam tiros e a visão da Bibliotecária) e seguram fantasmas
for i, ty in enumerate([14, 17, 20, 23]):
    L.prop(f"Estante{i+1}", "Biblioteca", 5 * T - bib[0], ty * T - bib[1], 8 * T, 128, WOOD, "", depth=64,
           tall=True, blocks_ghosts=True, sprite=f"estante{i}")
# xadrez gigante: o tapete azul e branco está no piso; as peças ficam em cima (algumas tombadas)
for i, (tx, ty, spr) in enumerate([(1, 25, "peca_torre"), (3, 26, "peca_rei"), (5, 25, "peca_cavalo"), (2, 28, "peca_peao"),
                                   (4, 29, "peca_caida"), (6, 27, "peca_caida_preta"), (0, 30, "peca_peao")]):
    big = spr.startswith("peca_caida")
    L.prop(f"Peca{i+1}", "Biblioteca", tx * T + 32 - bib[0], ty * T + 56 - bib[1], 80 if big else 48, 40 if big else 80,
           col(0, 0, 0, 0), solid=False, sprite=spr)
# mesas de estudo (baixas)
L.prop("MesaEstudo1", "Biblioteca", 14 * T - bib[0], 13 * T - bib[1], 128, 60, WHITE, depth=40, sprite="mesa_estudo0")
L.prop("MesaEstudo2", "Biblioteca", 14 * T - bib[0], 27 * T - bib[1], 128, 60, WHITE, depth=40, sprite="mesa_estudo1")
# luminárias (uma por sala de estudo)
L.node("Luminarias", "Biblioteca", type="Node2D", props={"y_sort_enabled": "true"})
for i, (a, b) in enumerate(STUDY_ROOMS):
    L.node(f"Luminaria{i+1}", "Biblioteca/Luminarias", inst="lamp",
           props={"position": v((a + b + 1) * T / 2 - bib[0], 4 * T + 48 - bib[1])})
# mesas de estudo dentro de cada sala + soleira de madeira nas portas (para parecer porta)
for i, (a, b) in enumerate(STUDY_ROOMS):
    L.prop(f"MesaSala{i+1}", "Biblioteca", (a + b + 1) * T / 2 - bib[0], 7 * T + 8 - bib[1], 150, 56, WHITE, depth=36,
           sprite="mesa_sala")
for i, x in enumerate(STUDY_DOORS):
    L.node(f"PortaSala{i+1}", type="ColorRect", props={"z_index": "-4", "offset_left": f"{x * T + 8}.0",
           "offset_top": f"{5 * T}.0", "offset_right": f"{x * T + 56}.0", "offset_bottom": f"{6 * T}.0",
           "mouse_filter": "2", "color": col(0.55, 0.38, 0.22, 1)})
# patrulha da Bibliotecária
L.node("Patrulha", "Biblioteca", type="Node2D")
for i, (tx, ty) in enumerate([(7, 11), (13, 10), (17, 19), (11, 22), (16, 29), (8, 30), (-1, 27), (10, 16), (-1, 12)]):
    x, y = px(tx, ty)
    L.node(f"Ponto{i+1}", "Biblioteca/Patrulha", type="Marker2D", props={"position": v(x - bib[0], y - bib[1])})
L.node("Bibliotecario", "Biblioteca", inst="librarian", props={"position": v(14 * T - bib[0], 20 * T - bib[1]),
       "Config": 'ExtResource("bib")', "Patrol": 'NodePath("../Patrulha")', "Lamps": 'NodePath("../Luminarias")'},
       node_paths=["Patrol", "Lamps"])
L.node("Canhao", inst="pickup", props={"position": v(14 * T, 21 * T), "ItemId": '"canhao"', "DisplayName": '"Canhão"',
       "Color": col(0.3, 0.9, 1, 1), "WeaponReward": 'ExtResource("cannon")', "StartsHidden": "true",
       "Texture": L.tex("canhao", "weapons")})
L.node("ChaveEscada", inst="pickup", props={"position": v(15 * T, 21 * T), "ItemId": '"chave_escada"',
       "DisplayName": '"Chave da escada"', "Color": col(1, 0.85, 0.2, 1), "StartsHidden": "true",
       "Texture": L.tex("chave_escada", "items"), "InspectTexture": L.tex("chave_escada", "items/inspecao"),
       "InspectTitle": '"CHAVE DA ESCADA"',
       "InspectText": '"Presa no cordão da Bibliotecária, com uma etiqueta vermelha: ESC. A escada fica no fim do corredor, ao lado do elevador quebrado."'})
L.prop("PlacaBiblioteca", ".", 19 * T + 32, 20 * T + 8, 180, 30, col(0, 0, 0, 0), "BIBLIOTECA", font=16,
       text_color=col(0.85, 0.8, 1), solid=False, z=1)

# ---------- elevador quebrado + escada: os dois só encostados na parede da esquerda, quase grudados ----------
L.prop("ElevadorQuebrado", ".", 24 * T + 24, -5 * T, 48, 3 * T, CONCRETE, depth=3 * T, tall=True,
       sprite="elevador_quebrado")
L.node("AvisoElevador", inst="item_slot", props={"position": v(25 * T, -6 * T), "Size": v(200, 220),
       "RequiredItem": '"nunca"', "MissingMessage": '"ELEVADOR - fora de serviço desde 2049. Use a escada aqui do lado."'})
L.prop("PortaEscada", ".", 24 * T + 24, -1 * T, 48, 3 * T, col(0.5, 0.42, 0.3), depth=3 * T, tall=True,
       sprite="porta_escada")
L.prop("PlacaEscada", ".", 25 * T + 32, -4 * T + 4, 140, 30, col(0, 0, 0, 0), "ESCADA", font=16,
       text_color=col(0.95, 0.9, 0.6), solid=False, z=1)
# encostar na escada com a chave leva à cena da escada; sem a chave, só a mensagem
L.node("Escada", inst="item_slot", props={"position": v(25 * T, -2.5 * T), "Size": v(200, 220),
       "RequiredItem": '"chave_escada"', "MissingMessage": '"ESCADA - trancada. A chave está com a Bibliotecária."',
       "UseMessage": '"A porta da escada abriu."'})

# ---------- decoração (só visual: não bloqueia nada, para não mexer na jogabilidade) ----------
L.prop("Armarios", ".", 22 * T + 32, 19 * T, 96, 120, col(0, 0, 0, 0), solid=False, sprite="armarios_azuis")
L.prop("LixeirasCorredor", ".", 29 * T, 27 * T, 84, 48, col(0, 0, 0, 0), solid=False, sprite="lixeiras", folder="props/fase1")
L.prop("PlacaMolhado1", ".", 25 * T + 16, 11 * T, 32, 44, col(0, 0, 0, 0), solid=False, sprite="placa_molhado")
L.prop("PlacaMolhado2", "Biblioteca", 9 * T - bib[0], 29 * T - bib[1], 32, 44, col(0, 0, 0, 0), solid=False, sprite="placa_molhado")
L.prop("BalcaoBib", "Biblioteca", 16 * T + 32 - bib[0], 17 * T - bib[1], 192, 80, col(0, 0, 0, 0), solid=False, sprite="balcao_bib")
for i, (tx, ty) in enumerate([(0, 11), (9, 11)]):
    L.prop(f"MesaRedonda{i+1}", "Biblioteca", tx * T - bib[0], ty * T + 40 - bib[1], 96, 72, col(0, 0, 0, 0), solid=False,
           sprite="mesa_redonda")
for i, (x, y, spr, folder) in enumerate([(26 * T, 33 * T, "cadeira_caida", "props/fase1"), (28 * T, 4 * T, "entulho", "props/fase1"),
                                         (25 * T + 20, 20 * T, "entulho", "props/fase1"), (15 * T, 39 * T + 40, "cadeira_espera_caida", "props/fase2")]):
    L.prop(f"Destrocos{i+1}", ".", x, y, 48, 32, col(0, 0, 0, 0), solid=False, sprite=spr, folder=folder)

# ---------- noite e chuva ----------
# Mesmo clima da Fase 1: escuro azulado (o jogador acende a lanterna), luzes falhando e a chuva lá fora.
# Aqui é tudo dentro do prédio: a chuva só aparece nos janelões, se ouve abafada e os relâmpagos clareiam tudo.
L.node("Noite", type="CanvasModulate", groups=["night"], props={"color": col(0.11, 0.12, 0.2, 1)})
L.node("NightOverlay", inst="night_overlay")
L.node("Chuva", inst="rain", props={"AlwaysInside": "true", "LightningInterval": v(16, 34)})
STEADY, FLICKER, BROKEN, PULSE = 0, 1, 2, 3
TUBO, FRIA = col(0.82, 0.92, 1), col(0.8, 0.88, 1)
L.node("Luzes", type="Node2D")
_lights = []


def light(name, x, y, color, scale, energy, mode, tube=True):
    _lights.append(name)
    p = {"position": v(x, y), "color": color, "texture_scale": f"{scale:g}", "BaseEnergy": f"{energy:g}",
         "Behavior": str(mode), "Seed": str(len(_lights) + 100)}
    if tube: p["texture"] = 'ExtResource("light_tubo")'
    L.node(name, "Luzes", inst="flicker_light", props=p)


# corredor: fluorescentes do teto, quase todas com defeito
for i, (ty, mode) in enumerate([(-6, BROKEN), (0, FLICKER), (6, BROKEN), (12, STEADY), (18, FLICKER), (24, BROKEN),
                                (30, FLICKER), (36, STEADY), (42, FLICKER)]):
    light(f"Corredor{i+1}", 27 * T, ty * T, TUBO, 2.0, 0.8, mode)
for i, (tx, mode) in enumerate([(6, BROKEN), (12, STEADY), (18, FLICKER)]):
    light(f"Hall{i+1}", tx * T, 37 * T, TUBO, 2.0, 0.85, mode)
light("EntradaBib", 21 * T + 32, 21 * T, TUBO, 1.6, 0.75, FLICKER)
light("Achados", 33 * T, 19 * T + 32, TUBO, 1.6, 0.7, FLICKER)
# biblioteca: escura (a Bibliotecária se guia pela luz das luminárias); duas lâmpadas dando estalos e a emergência
light("Biblioteca1", 5 * T, 19 * T, TUBO, 2.2, 0.8, BROKEN)
light("Biblioteca2", 12 * T, 27 * T, TUBO, 2.2, 0.8, BROKEN)
light("EmergenciaBib", 17 * T + 32, 21 * T, col(1, 0.15, 0.1), 1.4, 0.9, PULSE, tube=False)
# fim do corredor: a luz vermelha do elevador quebrado e a placa verde da escada
light("ElevadorQuebrado", 24 * T + 48, -6 * T, col(1, 0.15, 0.1), 1.6, 1.0, PULSE, tube=False)
light("PlacaEscada", 24 * T + 48, -3 * T, col(0.2, 1, 0.45), 0.8, 0.8, STEADY, tube=False)
light("BotaoElevador", 24 * T + 40, 46 * T, col(1, 0.5, 0.25), 0.6, 0.7, STEADY, tube=False)
saida_at = tuple(L.tiler.names["frente_interno_saida"])
for i, (c, a) in enumerate(sorted((c, a) for c, a in {**L.wall_tiles, **L.outer_wall_tiles}.items() if a == saida_at)):
    light(f"Saida{i+1}", c[0] * T + 32, c[1] * T + 72, col(0.2, 1, 0.45), 0.7, 0.8, STEADY, tube=False)

# ---------- jogador / interface ----------
L.node("Player", inst="player", props={"position": v(26 * T + 32, 45 * T + 32), "StartWithAllWeapons": "false"})
L.node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
L.node("MessageScreen", inst="message_screen")
L.node("ItemInspect", inst="item_inspect")

paths = {"Player": "Player", "Hud": "Hud", "Screen": "MessageScreen", "PortaBiblioteca": "Biblioteca/PortaBiblioteca",
         "Devolucao": "Devolucao", "PortaAchados": "PortaAchados", "Hall": "Hall", "Chave": "Chave", "Achados": "Achados",
         "Caixas": "Achados/Caixas", "Livro": "Livro", "Biblioteca": "Biblioteca",
         "Bibliotecario": "Biblioteca/Bibliotecario", "Canhao": "Canhao", "ChaveEscada": "ChaveEscada",
         "Escada": "Escada", "CentroHall": "CentroHall",
         "CadeirasDaSaida": "Hall/CadeirasDaSaida"}
L.write(sys.argv[1], root_node("Fase2", "fase2", paths))
print("chão", len(L.floor), "paredes dentro", len(L.inner_walls), "fora", len(L.outer_walls))
