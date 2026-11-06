# Gera levels/fase_1.tscn a partir da planta (tiles de 16px mostrados 4x = 64px).
import base64, struct, sys, os
sys.path.insert(0, os.path.dirname(__file__))
from tiling import Tiler, load_atlas, tile_data as atlas_tile_data, h
T = 64
OUT = sys.argv[1]

# Retângulos de chão (x0, y0, x1, y1) inclusivos, em tiles.
FLOOR = [
    (1, 52, 34, 61),   # Entrada (piso vermelho)
    (1, 62, 34, 63),   # calçada de fora (bloquete), com o portão
    (1, 64, 34, 65),   # avenida (asfalto)
    (21, 51, 34, 51),  # piso vermelho encosta na guarita (inclui a abertura da catraca)
    (23, 46, 24, 50),  # Portaria: passagem da catraca
    (23, 8, 30, 45),   # Corredor (largo)
    (2, 24, 21, 29),   # RU: salão
    (2, 30, 8, 39),    # RU: ala das mesas
    (11, 30, 13, 39),  # RU: passagem
    (14, 37, 22, 39),  # RU: entrada (vem do corredor)
    (2, 8, 21, 19),    # Cantina
    (22, 12, 22, 13),  # Cantina: entrada 1
    (22, 16, 22, 17),  # Cantina: entrada 2
    (2, 3, 3, 7),      # corredor do elevador (sobe)
    (4, 3, 6, 4),      # corredor do elevador (vira à direita)
    (7, 2, 10, 5),     # elevador
]
floor = set()
for x0, y0, x1, y1 in FLOOR:
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            floor.add((x, y))
walls = set()
for (x, y) in floor:
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            c = (x + dx, y + dy)
            if c not in floor:
                walls.add(c)

def tile_data(cells):
    b = bytearray(struct.pack('<H', 0))
    for (x, y), atlas in sorted(cells.items(), key=lambda i: (i[0][1], i[0][0])):
        b += struct.pack('<hhHHHH', x, y, 0, atlas, 0, 0)
    return base64.b64encode(bytes(b)).decode()


# ---------- material do piso de cada célula (o que a foto do campus mostra em cada lugar)
def material(x, y):
    if y >= 64: return "asfalto_faixa" if y == 65 and x % 3 == 1 else ("asfalto_meiofio" if y == 64 else "asfalto")
    if y == 63: return "bloquete_meiofio"
    if y == 62: return "bloquete"
    if y >= 51:  # Entrada: estacionamento de piso sextavado, caminho de bloquete do portão até a catraca
        if 16 <= x <= 20 or y <= 53 or (x >= 21 and y <= 53):
            return "bloquete_tatil" if (y == 57 and 16 <= x <= 20) else "bloquete"
        return "sextavado"
    if 23 <= x <= 30 and 8 <= y <= 45: return "praca"        # Corredor: praça aberta entre os prédios
    if 46 <= y <= 50: return "granito"                       # Portaria
    if 24 <= y <= 39 and x <= 22: return "ceramica"          # RU
    if 7 <= x <= 10 and 2 <= y <= 5: return "aco"            # elevador
    return "granito"                                         # Cantina e corredor do elevador
floor_mat = {c: material(*c) for c in floor}

# "fora do mapa": vazio ligado à borda (flood fill). Parede encostada nele = parede de fora.
xs = [c[0] for c in walls]; ys = [c[1] for c in walls]
bx0, bx1, by0, by1 = min(xs) - 1, max(xs) + 1, min(ys) - 1, max(ys) + 1
outside, stack = set(), [(bx0, by0)]
while stack:
    c = stack.pop()
    if c in outside or c in floor or c in walls: continue
    if not (bx0 <= c[0] <= bx1 and by0 <= c[1] <= by1): continue
    outside.add(c)
    stack += [(c[0] + 1, c[1]), (c[0] - 1, c[1]), (c[0], c[1] + 1), (c[0], c[1] - 1)]
def is_outer(c):
    return any((c[0] + dx, c[1] + dy) in outside for dx in (-1, 0, 1) for dy in (-1, 0, 1))
def wall_tile(c):
    return 3 if (c[0], c[1] + 1) in floor else 2

def deco_rule(c, style, below):
    """Decoração da frente da parede: janelões no RU e na cantina, extintor, saída, mural, ar-condicionado."""
    x, y = c
    r = h("deco", x, y)
    if style == "interno":
        if below == "ceramica" and y == 23: return "janela" if x % 4 != 1 else None
        if below == "granito" and y == 7 and x in (6, 15): return "mural"
        if r < 0.07: return "extintor"
        if r < 0.12: return "saida"
        return None
    if style == "fachada":
        return "ar" if r < 0.15 else ("lisa" if r < 0.25 else None)
    return None

outer_set = {c for c in walls if is_outer(c)}
tiler = Tiler(load_atlas("fase1"), floor_mat, walls, outer_set, deco_rule)
floor_cells = tiler.floor_cells()
wall_cells = tiler.wall_cells(only_outer=False)
outer_cells = tiler.wall_cells(only_outer=True)
shadow_cells = tiler.shadow_cells()
tile_data = atlas_tile_data
print("paredes de dentro", len(wall_cells), "de fora", len(outer_cells))

def px(tx, ty):  # centro do tile
    return (tx * T + T // 2, ty * T + T // 2)

def v(x, y):
    return f"Vector2({x:g}, {y:g})"

def col(r, g, b, a=1):
    return f"Color({r:g}, {g:g}, {b:g}, {a:g})"

nodes = []

def node(name, parent=".", type=None, inst=None, props=None, groups=None, node_paths=None):
    head = f'[node name="{name}"'
    if type: head += f' type="{type}"'
    head += f' parent="{parent}"'
    if node_paths: head += ' node_paths=PackedStringArray(' + ', '.join(f'"{p}"' for p in node_paths) + ')'
    if groups: head += ' groups=[' + ', '.join(f'"{g}"' for g in groups) + ']'
    if inst: head += f' instance=ExtResource("{inst}")'
    head += ']'
    lines = [head]
    for k, val in (props or {}).items():
        lines.append(f"{k} = {val}")
    nodes.append("\n".join(lines))

CONCRETE = col(0.62, 0.62, 0.6)
WHITE = col(0.78, 0.79, 0.8)
BEGE = col(0.66, 0.6, 0.5)
BLACK = col(0.14, 0.14, 0.16)
GREEN = col(0.25, 0.55, 0.35)
DARK_TEXT = col(0.08, 0.08, 0.1)

TEXTURES = []  # sprites usados pelos props (viram ext_resource no fim)


def tex(name, folder="props/fase1"):
    path = f"res://sprites/{folder}/{name}.png"
    if path not in TEXTURES: TEXTURES.append(path)
    return f'ExtResource("tex_{name}")'


def prop(name, parent, x, y, w, h, color, text="", depth=None, font=16, text_color=None, solid=True, z=None, tall=False,
         boss_counter=False, blocks_ghosts=False, inner=None, sprite=None):
    p = {"position": v(x, y), "Size": v(w, h), "Color": color}
    if sprite: p["Texture"] = tex(sprite)
    if inner: p["InnerColor"] = inner
    if blocks_ghosts: p["BlocksGhosts"] = "true"
    if tall: p["Tall"] = "true"
    if boss_counter: p["BossCounter"] = "true"
    if depth is not None: p["Depth"] = f"{depth:g}"
    if text:
        p["Text"] = '"' + text.replace('"', '\\"') + '"'
        p["FontSize"] = str(font)
    if text_color: p["TextColor"] = text_color
    if not solid: p["Solid"] = "false"
    if z is not None: p = {"z_index": str(z), **p}
    node(name, parent, inst="prop", props=p)

# --- raiz
root_paths = ["Player", "Hud", "Screen", "Cracha", "Pilhas", "Catraca", "Ru", "PortaEntradaRu", "Marmita", "Microondas",
              "Corredor", "Cantina", "CantinaPorta1", "CantinaPorta2", "Tia", "Escopeta", "PortaElevador", "Elevador"]
root_head = ('[node name="Fase1" type="Node2D" node_paths=PackedStringArray(' +
             ', '.join(f'"{p}"' for p in root_paths) + ')]')
root_props = {"y_sort_enabled": "true", "script": 'ExtResource("fase1")',
              "Player": 'NodePath("Player")', "Hud": 'NodePath("Hud")', "Screen": 'NodePath("MessageScreen")',
              "Cracha": 'NodePath("Cracha")', "Pilhas": 'NodePath("Pilhas")', "Catraca": 'NodePath("Catraca")', "Ru": 'NodePath("RU")', "PortaEntradaRu": 'NodePath("PortaEntradaRU")',
              "Marmita": 'NodePath("RU/Marmita")', "Microondas": 'NodePath("RU/MicroondasEncaixe")',
              "Corredor": 'NodePath("Corredor")', "Cantina": 'NodePath("Cantina")',
              "CantinaPorta1": 'NodePath("Cantina/Entrada1")', "CantinaPorta2": 'NodePath("Cantina/Entrada2")',
              "Tia": 'NodePath("Cantina/Tia")', "Escopeta": 'NodePath("Escopeta")',
              "PortaElevador": 'NodePath("PortaElevador")', "Elevador": 'NodePath("Elevador")'}
nodes.append(root_head + "\n" + "\n".join(f"{k} = {val}" for k, val in root_props.items()))

node("Floor", type="TileMapLayer", groups=["floor"], props={"z_index": "-10", "texture_filter": "1", "scale": v(4, 4),
     "tile_map_data": f'PackedByteArray("{tile_data(floor_cells)}")', "tile_set": 'ExtResource("tiles")'})

# fundo: o que fica fora das salas. Perto da rua e da praça é grama; perto das salas, a laje dos prédios.
_bx0, _bx1 = min(c[0] for c in walls) - 6, max(c[0] for c in walls) + 6
_by0, _by1 = min(c[1] for c in walls) - 6, max(c[1] for c in walls) + 6
background = {}
for y in range(_by0, _by1 + 1):
    for x in range(_bx0, _bx1 + 1):
        c = (x, y)
        if c in floor or c in walls: continue
        inside_buildings = 1 <= x <= 22 and 1 <= y <= 51   # bloco do RU, cantina e elevador
        background[c] = tiler.at(f"laje{int(h('l', *c) * 3)}") if inside_buildings else tiler.at(f"grama{int(h('g', *c) * 3)}")
node("Fundo", type="TileMapLayer", props={"z_index": "-11", "texture_filter": "1", "scale": v(4, 4),
     "tile_map_data": f'PackedByteArray("{tile_data(background)}")', "tile_set": 'ExtResource("tiles")'})

# sombras de contato: escurecem o chão junto às paredes
node("Sombras", type="TileMapLayer", props={"z_index": "-9", "texture_filter": "1", "scale": v(4, 4),
     "tile_map_data": f'PackedByteArray("{tile_data(shadow_cells)}")', "tile_set": 'ExtResource("tiles")'})

node("Walls", type="TileMapLayer", groups=["obstacles"], props={"y_sort_enabled": "true", "texture_filter": "1",
     "scale": v(4, 4), "tile_map_data": f'PackedByteArray("{tile_data(wall_cells)}")', "tile_set": 'ExtResource("tiles")'})
# paredes da borda do mapa: mesma aparência, mas também seguram os fantasmas
node("OuterWalls", type="TileMapLayer", groups=["obstacles", "outer_walls"], props={"y_sort_enabled": "true",
     "texture_filter": "1", "scale": v(4, 4), "tile_map_data": f'PackedByteArray("{tile_data(outer_cells)}")',
     "tile_set": 'ExtResource("tiles_outer")'})

# ===== ENTRADA =====
prop("Predio", ".", 11 * T, 54 * T, 20 * T, 240, CONCRETE, depth=128, tall=True, sprite="predio")
# o letreiro "U IFE P" faz parte do desenho do prédio (sprites/props/fase1/predio.png)
# letras caídas: deitadas no chão, não bloqueiam nada
prop("LetraN", ".", 8 * T, 57 * T + 40, 96, 96, col(0, 0, 0, 0), "N", font=84,
     text_color=col(0.58, 0.58, 0.56), solid=False, z=-4, sprite="letra_n")
prop("LetraS", ".", 15 * T, 58 * T + 40, 96, 96, col(0, 0, 0, 0), "S", font=84,
     text_color=col(0.58, 0.58, 0.56), solid=False, z=-4, sprite="letra_s")
# crachá escondido: aparece debaixo da pilha sorteada
node("Cracha", inst="pickup", props={"position": v(11 * T, 57 * T), "ItemId": '"cracha"', "DisplayName": '"Crachá"',
     "Color": col(0.4, 0.75, 1, 1), "StartsHidden": "true",
     "Texture": tex("cracha", "items")})
node("Pilhas", type="Node2D", props={"y_sort_enabled": "true"})
for i, (tx, ty) in enumerate([(5, 56), (11, 59), (19, 56), (24, 60), (28, 57), (33, 59),
                               (8, 55), (15, 55), (23, 54), (31, 54)]):
    node(f"Pilha{i+1}", "Pilhas", inst="paper_pile", props={"position": v(tx * T + 32, ty * T + 48)})

# ===== PORTÃO (separa a calçada da entrada) =====
GRADE = col(0.5, 0.52, 0.56)
prop("GradeEsquerda", ".", 8 * T + 32, 63 * T, 15 * T, 72, GRADE, depth=24, sprite="gradil")
prop("GradeDireita", ".", 27 * T + 32, 63 * T, 15 * T, 72, GRADE, depth=24, sprite="gradil")
prop("PostePortao1", ".", 16 * T, 63 * T, 28, 128, col(0.4, 0.42, 0.46), depth=24, sprite="poste_portao")
prop("PostePortao2", ".", 20 * T, 63 * T, 28, 128, col(0.4, 0.42, 0.46), depth=24, sprite="poste_portao")
prop("PlacaPortao", ".", 18 * T, 63 * T - 96, 260, 36, col(0, 0, 0, 0), "UNIFESP  ·  ICT", font=20,
     text_color=col(0.9, 0.9, 0.9), solid=False, z=1, sprite="placa_portao")
# ponto de ônibus verde da avenida, do lado de fora do portão
prop("PontoOnibus", ".", 30 * T, 64 * T - 8, 192, 136, col(0, 0, 0, 0), depth=20, sprite="ponto_onibus")

# ===== PORTARIA =====
# guarita do porteiro: bloco grande à esquerda da passagem estreita
prop("Guarita", ".", 30 * T, 51 * T, 10 * T, 5 * T, BEGE, "PORTEIRO\n\n[ janela ]", depth=5 * T, font=18, tall=True,
     blocks_ghosts=True, sprite="guarita")
node("Catraca", inst="door", props={"position": v(24 * T, 49 * T), "Size": v(2 * T, 64), "StartsLocked": "true",
     "Color": col(0.95, 0.8, 0.3, 1), "RequiredItem": '"cracha"',
     "LockedMessage": '"Acesso negado: precisa de crachá."', "UnlockMessage": '"BIP! Acesso liberado."',
     "LockedSprite": tex("catraca_trancada", "objects"), "UnlockedSprite": tex("catraca_aberta", "objects")})

# ===== CORREDOR =====
cor = (23 * T, 8 * T)
node("Corredor", inst="room", props={"position": v(*cor), "Size": v(8 * T, 38 * T), "Waves": "3",
     "EnemiesPerWave": "6", "SpawnAnywhere": "true", "Active": "false", "RespawnPoint": 'NodePath("Respawn")'}, node_paths=["RespawnPoint"])
node("Respawn", "Corredor", type="Marker2D", props={"position": v(24 * T - cor[0], 47 * T + 32 - cor[1])})
# dois jardins no meio do corredor (borda de concreto, miolo verde)
for i, (top, bottom) in enumerate([(14, 19), (30, 35)]):
    h = (bottom - top + 1) * T
    prop(f"Jardim{i+1}", "Corredor", 27 * T - cor[0], (bottom + 1) * T - cor[1], 2 * T, h, CONCRETE, depth=h,
         inner=col(0.22, 0.42, 0.22), sprite="jardim")
# postes brancos de globo ao longo da praça
for i, (tx, ty) in enumerate([(23, 11), (30, 11), (23, 24), (30, 24), (23, 38), (30, 38)]):
    prop(f"Poste{i+1}", "Corredor", tx * T + 32 - cor[0], ty * T + 48 - cor[1], 32, 176, col(0, 0, 0, 0), depth=12,
         sprite="poste_luz")
for i, ty in enumerate([10, 15, 20, 26, 31, 41, 44]):
    x, y = px(24 + 5 * (i % 2), ty)
    node(f"Spawn{i+1}", "Corredor", type="Marker2D", props={"position": v(x - cor[0], y - cor[1])})
# ===== RU =====
ru = (2 * T, 24 * T)
node("RU", inst="room", props={"position": v(*ru), "Size": v(20 * T, 16 * T), "Waves": "0",
     "SurviveTime": "50.0", "SurviveSpawnInterval": "1.2", "SurviveMaxAlive": "7", "SpawnAnywhere": "true", "StartOnEnter": "false",
     "RespawnPoint": 'NodePath("Respawn")'}, node_paths=["RespawnPoint"])
node("Porta", "RU", inst="door", props={"position": v(12 * T + 32 - ru[0], 31 * T - ru[1]), "Size": v(3 * T, 64)})
node("Respawn", "RU", type="Marker2D", props={"position": v(12 * T + 32 - ru[0], 28 * T - ru[1])})
for i, (tx, ty) in enumerate([(3, 25), (20, 25), (3, 28), (20, 28), (9, 26), (4, 33), (6, 38)]):
    x, y = px(tx, ty)
    node(f"Spawn{i+1}", "RU", type="Marker2D", props={"position": v(x - ru[0], y - ru[1])})
prop("BalcaoPrato", "RU", 4 * T + 32 - ru[0], 25 * T - ru[1], 5 * T, 88, BEGE, "deixa o prato", depth=56,
     blocks_ghosts=True, sprite="balcao_devolucao")
prop("MicroondasBloco", "RU", 11 * T - ru[0], 25 * T - ru[1], 112, 96, col(0.85, 0.85, 0.88), "MICRO-\nONDAS",
     depth=56, font=14, blocks_ghosts=True, sprite="microondas")
node("MicroondasEncaixe", "RU", inst="item_slot", props={"position": v(11 * T - ru[0], 25 * T - ru[1]),
     "Size": v(224, 160), "RequiredItem": '"marmita"', "MissingMessage": '"Você precisa de uma marmita."',
     "UseMessage": '"Micro-ondas ligado... os fantasmas sentiram o cheiro!"'})
# ilha "pegar comida" no meio do salão (dá para rodar em volta); a marmita fica em cima dela
prop("BalcaoComida", "RU", 18 * T - ru[0], 26 * T - ru[1], 4 * T, 96, BEGE, "pegar comida", depth=64,
     blocks_ghosts=True, sprite="balcao_comida")
node("Marmita", "RU", inst="pickup", props={"position": v(18 * T - ru[0], 26 * T + 8 - ru[1]),
     "ItemId": '"marmita"', "DisplayName": '"Marmita"', "Color": col(1, 0.6, 0.25, 1),
     "Texture": tex("marmita", "items")})
for i, ty in enumerate([31, 33, 35, 37]):
    prop(f"Mesa{i+1}", "RU", 5 * T + 32 - ru[0], (ty + 1) * T - ru[1], 3 * T, 64, WHITE, depth=44, sprite="mesa_ru")
    prop(f"CadeiraRU{i+1}", "RU", 8 * T + 8 - ru[0], (ty + 1) * T - 4 - ru[1], 32, 48, col(0, 0, 0, 0), solid=False,
         sprite="cadeira_ru")
prop("LixeirasRU", "RU", 15 * T - ru[0], 24 * T + 44 - ru[1], 84, 48, col(0, 0, 0, 0), solid=False, sprite="lixeiras")
# primeira porta: entre o corredor e a entradinha do RU (tranca quando o corredor fica assombrado)
node("PortaEntradaRU", inst="door", props={"position": v(22 * T + 32, 40 * T), "Size": v(64, 3 * T)})

# ===== CANTINA =====
ca = (2 * T, 8 * T)
node("Cantina", inst="room", props={"position": v(*ca), "Size": v(20 * T, 12 * T), "Waves": "0",
     "HasBoss": "true", "RespawnPoint": 'NodePath("Respawn")'}, node_paths=["RespawnPoint"])
for name, ty in (("Entrada1", 14), ("Entrada2", 18)):
    node(name, "Cantina", inst="door", props={"position": v(22 * T + 32 - ca[0], ty * T - ca[1]),
         "Size": v(64, 2 * T), "StartsLocked": "true"})
node("Respawn", "Cantina", type="Marker2D", props={"position": v(25 * T - ca[0], 13 * T + 32 - ca[1])})
for i, (tx, ty) in enumerate([(2, 13), (2, 18), (21, 15), (21, 19), (9, 19), (17, 19)]):
    x, y = px(tx, ty)
    node(f"Spawn{i+1}", "Cantina", type="Marker2D", props={"position": v(x - ca[0], y - ca[1])})
# Tia da Cantina: logo na frente do balcão, no centro
# Tia da Cantina: dentro do balcão fechado
node("Tia", "Cantina", inst="boss", props={"position": v(13 * T - ca[0], 9 * T + 52 - ca[1]),
     "Config": 'ExtResource("tia")', "Targets": 'NodePath("../Alvos")', "MoveRange": "460.0"}, node_paths=["Targets"])
node("Alvos", "Cantina", type="Node2D", props={"y_sort_enabled": "true"})
for i, (tx, ty) in enumerate([(2, 11), (21, 12), (3, 17), (20, 18)]):
    node(f"Alvo{i+1}", "Cantina/Alvos", inst="boss_target", props={"position": v(tx * T + 32 - ca[0], ty * T + 48 - ca[1])})
# balcão fechado: frente de parede a parede + lateral esquerda (não dá para passar por trás)
prop("Balcao", "Cantina", 13 * T - ca[0], 11 * T - ca[1], 18 * T, 96, BEGE, "C A N T I N A", depth=64, font=22,
     boss_counter=True, blocks_ghosts=True, sprite="balcao_cantina")
prop("BalcaoLateral", "Cantina", 4 * T + 32 - ca[0], 10 * T - ca[1], 64, 2 * T, BEGE, depth=2 * T, boss_counter=True,
     blocks_ghosts=True, sprite="balcao_lateral")
prop("LetreiroCantina", "Cantina", 13 * T - ca[0], 8 * T + 16 - ca[1], 360, 48, col(0, 0, 0, 0), "UNIFESP",
     font=30, text_color=col(0.35, 1, 0.55), solid=False, sprite="letreiro_cantina")  # sem z: fica atrás da Tia
for i, (tx, ty) in enumerate([(6, 13), (16, 13), (9, 15), (14, 16)]):
    prop(f"Mesa{i+1}", "Cantina", tx * T + 32 - ca[0], ty * T - ca[1], 96, 64, WHITE, depth=44, sprite="mesa_cantina")
prop("Sinuca1", "Cantina", 6 * T - ca[0], 19 * T + 16 - ca[1], 3 * T, 80, col(0.15, 0.4, 0.25), "sinuca",
     depth=56, text_color=col(0.7, 0.9, 0.75), sprite="sinuca")
prop("Sinuca2", "Cantina", 13 * T - ca[0], 19 * T + 16 - ca[1], 3 * T, 80, col(0.15, 0.4, 0.25), "sinuca",
     depth=56, text_color=col(0.7, 0.9, 0.75), sprite="sinuca")
# canto de convivência: lixeiras da coleta seletiva, bebedouro e sofá
prop("LixeirasCantina", "Cantina", 19 * T - ca[0], 12 * T + 8 - ca[1], 84, 48, col(0, 0, 0, 0), solid=False, sprite="lixeiras")
prop("Bebedouro", "Cantina", 21 * T + 8 - ca[0], 12 * T + 8 - ca[1], 32, 64, col(0, 0, 0, 0), solid=False, sprite="bebedouro")
prop("Sofa", "Cantina", 18 * T + 32 - ca[0], 19 * T + 56 - ca[1], 128, 56, col(0, 0, 0, 0), solid=False, sprite="sofa")

node("Escopeta", inst="pickup", props={"position": v(13 * T, 12 * T), "ItemId": '"escopeta"',
     "DisplayName": '"Escopeta"', "Color": col(0.95, 0.45, 0.2, 1), "WeaponReward": 'ExtResource("shotgun")',
     "StartsHidden": "true", "Texture": tex("escopeta", "items")})

# ===== ELEVADOR =====
node("PortaElevador", inst="door", props={"position": v(3 * T, 8 * T), "Size": v(2 * T, 64), "StartsLocked": "true",
     "LockedMessage": '"Trancado. Derrote a Tia da Cantina."'})
node("Elevador", inst="area_trigger", props={"position": v(7 * T, 2 * T), "Size": v(4 * T, 4 * T),
     "Color": col(1, 0.85, 0.3, 0.08), "Text": '""'})
prop("PortaElevador1", ".", 9 * T, 2 * T, 128, 64, col(0, 0, 0, 0), solid=False, z=1, sprite="porta_elevador")
# caixa d'água verde no gramado ao lado da praça (aparece de quase todo o campus)
prop("CaixaDagua", ".", 34 * T, 30 * T, 80, 288, col(0, 0, 0, 0), solid=False, sprite="caixa_dagua")

# ===== JOGADOR / HUD =====
node("Player", inst="player", props={"position": v(18 * T, 64 * T + 16), "StartWithAllWeapons": "false"})
node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
node("MessageScreen", inst="message_screen")

ext = [
    ('Script', 'res://levels/Fase1.cs', 'fase1'),
    ('TileSet', 'res://tilesets/fase1_tileset.tres', 'tiles'),
    ('TileSet', 'res://tilesets/fase1_tileset_outer.tres', 'tiles_outer'),
    ('PackedScene', 'res://objects/prop/prop.tscn', 'prop'),
    ('PackedScene', 'res://objects/pickup/pickup.tscn', 'pickup'),
    ('PackedScene', 'res://objects/paper_pile/paper_pile.tscn', 'paper_pile'),
    ('PackedScene', 'res://objects/door/door.tscn', 'door'),
    ('PackedScene', 'res://objects/room/room.tscn', 'room'),
    ('PackedScene', 'res://objects/item_slot/item_slot.tscn', 'item_slot'),
    ('PackedScene', 'res://objects/area_trigger/area_trigger.tscn', 'area_trigger'),
    ('PackedScene', 'res://objects/player/player.tscn', 'player'),
    ('PackedScene', 'res://objects/hud/hud.tscn', 'hud'),
    ('PackedScene', 'res://objects/message_screen/message_screen.tscn', 'message_screen'),
    ('PackedScene', 'res://objects/boss/boss.tscn', 'boss'),
    ('PackedScene', 'res://objects/boss/boss_target.tscn', 'boss_target'),
    ('Resource', 'res://objects/boss/tia_cantina.tres', 'tia'),
    ('Resource', 'res://objects/weapon/shotgun.tres', 'shotgun'),
]
with open(OUT, "w") as f:
    f.write("[gd_scene format=4]\n\n")
    ext += [('Texture2D', p, "tex_" + p.rsplit("/", 1)[1][:-4]) for p in TEXTURES]
    for t, p, i in ext:
        f.write(f'[ext_resource type="{t}" path="{p}" id="{i}"]\n')
    f.write("\n" + "\n\n".join(nodes) + "\n")
print("floor", len(floor), "walls", len(walls))
