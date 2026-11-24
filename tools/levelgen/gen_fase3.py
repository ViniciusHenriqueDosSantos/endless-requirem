# Gera levels/fase_3.tscn (4º andar: laboratórios) a partir da planta do João (FASE_3_NOVA).
# Uso: python3 tools/levelgen/gen_fase3.py levels/fase_3.tscn
#
# MVP 4a: a planta, os móveis principais e a luz.
# MVP 5a/5b/5c: a porta de vidro da área final com a fechadura de senha, o alarme (fantasmas no corredor),
#   os laboratórios como áreas seguras, o computador do Lab 2 e o quadro de energia do Lab 1 (roteiro em Fase3.cs).
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
# MVP 6: a convivência subiu CY linhas para caber o corredor de vidro entre ela e o corredor de baixo.
CY = -5


def up(r):
    return (r[0], r[1] + CY, r[2], r[3] + CY)


CONV_PARTES = [up(p) for p in [(7, 0, 24, 26),      # bloco principal
                               (0, 0, 6, 9),        # parte de cima à esquerda (o tatame)
                               (0, 20, 6, 26),      # pedaço de baixo à esquerda
                               (25, 9, 29, 16)]]    # avanço à direita
CONV = up((0, 0, 29, 26))      # retângulo que contém a convivência inteira
TATAME = up((0, 0, 13, 9))     # canto de cima à esquerda, onde os alunos descansam
# planta NOVA_AREA_FINAL: corredor de vidro (estreito) da convivência até o corredor/hall dos elevadores,
# e, saindo de baixo do avanço da direita, um corredor em L até o auditório (com o palco embaixo)
VIDRO = (5, CONV[3] + 1, 6, 26)
LIGACAO = [up((26, 17, 27, 19)), up((28, 18, 31, 19))]
AUD = up((32, 9, 42, 23))      # auditório (plateia), 11 x 15
PALCO = up((32, 24, 42, 25))   # palco, embaixo do auditório
CORR = (4, 27, 7, 75)          # corredor (4 de largura), sai de baixo da convivência
HALL = (9, 28, 11, 36)         # hall estreito dos elevadores (direita)
LAB2 = (9, 43, 20, 53)         # laboratório 2 (direita)
LAB1 = (-9, 57, 2, 67)         # laboratório 1 (esquerda)
ENTRADA = (9, 68, 11, 75)      # chegada: escada e elevador (mesma largura do hall)

for r in CONV_PARTES + [CORR, HALL, LAB2, LAB1, ENTRADA, VIDRO, AUD, PALCO] + LIGACAO:
    L.add_floor(*r)
L.add_floor(8, 28, 8, 36)      # hall aberto para o corredor (na altura toda)
L.add_floor(8, 44, 8, 45)      # porta do laboratório 2 (pequena, no alto da sala)
L.add_floor(3, 58, 3, 59)      # porta do laboratório 1 (pequena, no alto da sala)
L.add_floor(8, 68, 8, 75)      # entrada aberta para o corredor (na altura toda)
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
L.res('Script', 'res://objects/fase3/Interactable.cs', 'interactable')
L.res('Script', 'res://objects/fase3/GlowingDevice.cs', 'glow')
L.res('Script', 'res://objects/fase3/SafeZone.cs', 'safe_zone')
L.res('PackedScene', 'res://objects/area_trigger/area_trigger.tscn', 'area_trigger')

# ---------- arte do piso e das paredes (mesmo tileset das Fases 1 e 2) ----------
from tiling import h


def inside(r, x, y):
    return r[0] <= x <= r[2] and r[1] <= y <= r[3]


def material(x, y):
    if inside(LAB1, x, y) or inside(LAB2, x, y): return "ceramica"   # laboratórios: piso de cerâmica
    if any(inside(p, x, y) for p in CONV_PARTES + [AUD, PALCO] + LIGACAO): return "vinil"                              # convivência: vinílico
    return "granito"                                                   # corredor, hall e entrada


floor_mat = {c: material(*c) for c in L.floor}


def deco_rule(c, style, below):
    x, y = c
    r = h("deco3", x, y)
    if style != "interno": return None
    if y == CONV[1] - 1: return "janelao_quebrado" if r < 0.3 else "janelao"       # janelões no fundo da convivência
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
# tatame: placas de EVA azuis e verdes no chão (2x2 tiles cada, sprite com os dentes de encaixe) e puffs coloridos
L.node("Tatame", type="Node2D")
for ty in range(TATAME[1], TATAME[3] + 1, 2):
    for tx in range(TATAME[0], TATAME[2] + 1, 2):
        cor = "eva_azul" if ((tx + ty) // 2) % 2 == 0 else "eva_verde"
        L.prop(f"Placa_{tx}_{ty}", "Tatame", (tx + 1) * T, (ty + 2) * T, 2 * T, 2 * T, col(0, 0, 0, 0), solid=False,
               sprite=cor, folder="props/fase3", z=-5)
# puffs em grupinhos encostados nas bordas do tatame (cantos e paredes), o meio fica livre
for i, (tx, ty, cor) in enumerate([(0.6, 0.4, "vermelho"), (1.7, 0.5, "azul"), (0.5, 1.6, "amarelo"),
                                   (7.5, 0.4, "verde"), (8.7, 0.5, "laranja"),
                                   (0.5, 7.4, "roxo"), (0.6, 8.5, "rosa"), (1.8, 8.6, "turquesa")]):
    L.prop(f"Puff{i+1}", ".", tx * T + 32, (ty + CY) * T + 60, 72, 56, col(0, 0, 0, 0), depth=28,
           sprite=f"puff_{cor}", folder="props/fase3")
L.prop("PlacaTatame", ".", 4 * T + 32, (TATAME[1] + 2) * T + 40, 260, 30, col(0, 0, 0, 0), "TATAME", font=16,
       text_color=col(0.75, 0.9, 1), solid=False, z=1)

# cinco mesas do mesmo estilo espalhadas pela área (a primeira com 3 cadeiras, as outras com 4);
# a última fica no avanço da direita
for i, (tx, ty, chairs) in enumerate([(16, 6, "NSO"), (22, 6, "NSOL"), (21, 11, "NSOL"), (14, 14, "NSOL"),
                                      (27, 12, "NSOL")]):
    cx, cy = tx * T + 32, (ty + CY) * T + 48
    L.prop(f"Mesa{i+1}", ".", cx, cy, 96, 64, WOOD, depth=40, sprite="mesa_cantina", folder="props/fase1")
    spots = {"N": (cx, cy - 72), "S": (cx, cy + 60), "O": (cx - 76, cy + 8), "L": (cx + 76, cy + 8)}
    for side in chairs:
        x, y = spots[side]
        L.prop(f"Mesa{i+1}Cadeira{side}", ".", x, y, 36, 46, CHAIR, depth=22, sprite="cadeira_espera")

# duas mesas de ping-pong: mais para baixo e para a direita (sem encostar na parede de baixo)
for i, ty in enumerate([19, 22]):
    L.prop(f"PingPong{i+1}", ".", 20 * T + 32, (ty + CY) * T + 8, 240, 124, col(0.1, 0.38, 0.28), depth=64,
           sprite="pingpong", folder="props/fase3")

# parede de baixo: o pallet com almofadas à esquerda do corredor; à direita, uma fileira de cadeiras (iguais às das
# mesas) todas coladas, viradas para o ping-pong e, mais para a direita, as lixeiras
L.prop("Pallet", ".", 2 * T, BOTTOM - 4, 3 * T + 16, 64, col(0.55, 0.42, 0.26), depth=40,
       inner=col(0.75, 0.35, 0.3))
for i in range(10):
    L.prop(f"CadeiraFileira{i+1}", ".", 12 * T + 20 + i * 40, BOTTOM - 4, 36, 46, CHAIR, depth=22,
           sprite="cadeira_espera")
L.prop("LixeirasConv", ".", 23 * T + 32, BOTTOM - 4, 84, 48, col(0, 0, 0, 0), solid=False, sprite="lixeiras",
       folder="props/fase1")

# ---------- corredor ----------
L.prop("LixeirasCorredor", ".", 8 * T - 48, 39 * T, 84, 48, col(0, 0, 0, 0), solid=False, sprite="lixeiras",
       folder="props/fase1")
L.nodes[-1] += "\nrotation = 1.5708"     # girada 90°, encostada na parede da direita
# armários encostados na parede da esquerda do corredor, entre os dois laboratórios
for i, ty in enumerate(range(45, 55, 2)):
    L.prop(f"Armario{i+1}", ".", 4 * T + 22, (ty + 2) * T - 4, 44, 2 * T - 8, col(0.25, 0.4, 0.6), depth=2 * T - 8,
           tall=True, blocks_ghosts=True, sprite=f"armario{(i * 2) % 3}", folder="props/fase3")   # fantasma não atravessa

# ---------- hall estreito: dois elevadores na parede da direita (não são usados) ----------
for i, feet in enumerate([32, 36]):
    L.prop(f"ElevadorHall{i+1}", ".", HALL[2] * T + 40, (feet + 1) * T, 48, 3 * T, CONCRETE, depth=3 * T, solid=False,
           sprite="elevador", flip=True)
    L.node(f"AvisoElevadorHall{i+1}", inst="item_slot", props={"position": v(HALL[2] * T, (feet - 0.5) * T),
           "Size": v(140, 200), "RequiredItem": '"nunca"', "MissingMessage": '"ELEVADOR - sem energia neste andar."'})
L.prop("PlacaHall", ".", 10 * T + 32, HALL[1] * T + 4, 160, 26, col(0, 0, 0, 0), "ELEVADORES", font=13,
       text_color=col(0.9, 0.9, 0.85), solid=False, z=1)

# ---------- laboratórios: duas fileiras verticais de computadores, com cadeiras do lado ----------
PC = col(0.75, 0.75, 0.72)


def laboratorio(name, r, door_x, door_y, label_x, wall_side):
    ox, oy = r[0] * T, r[1] * T
    L.node(name, inst="room", props={"position": v(ox, oy), "Size": v((r[2] - r[0] + 1) * T, (r[3] - r[1] + 1) * T),
           "Waves": "0"})
    width = r[2] - r[0] + 1
    top, bottom = r[1] + 2, r[3]              # bancadas vão quase até embaixo (um tile livre para circular)
    # três fileiras com a mesma distância entre elas: uma encostada na parede de fora (cadeiras só do lado da sala)
    # e duas no meio (cadeiras dos dois lados); o espaço livre maior fica do lado da porta
    STEP = 250
    both = (("E", -64), ("D", 64))
    if wall_side == "esquerda":
        edge = r[0] * T
        rows = [(edge + 36, (("D", 64),)), (edge + 36 + STEP, both), (edge + 36 + 2 * STEP, both)]
    else:
        edge = (r[2] + 1) * T
        rows = [(edge - 36, (("E", -64),)), (edge - 36 - STEP, both), (edge - 36 - 2 * STEP, both)]
    for i, (bx, sides) in enumerate(rows):
        kind = "dupla" if len(sides) == 2 else ("esq" if sides[0][0] == "E" else "dir")   # para onde os PCs olham
        L.prop(f"Bancada{i+1}", name, bx - ox, bottom * T - oy, 72, (bottom - top) * T, PC, "", depth=(bottom - top) * T,
               sprite=f"bancada_{kind}", folder="props/fase3")
        # cadeiras do lado da bancada, uma a cada computador
        for j, ty in enumerate(range(top, bottom, 2)):
            for side, dx in sides:
                if (j + (0 if side == "E" else 1)) % 3 == 2: continue   # algumas cadeiras faltando (abandono)
                L.prop(f"Cadeira{i+1}{side}{j+1}", name, bx + dx - ox, ty * T + 56 - oy, 36, 46, CHAIR, depth=22,
                       sprite="cadeira_espera")
    # uma lousa branca colada na parede de cima, ocupando quase a parede toda
    L.prop("Lousa", name, (r[0] + r[2] + 1) * T / 2 - ox, r[1] * T + 4 - oy, (width - 2) * T, 64,
           col(0.93, 0.94, 0.95), solid=False, sprite=f"lousa{name[-1]}", folder="props/fase3")
    L.prop(f"Placa{name}", ".", label_x * T, r[1] * T - 40, 240, 26, col(0, 0, 0, 0), "LABORATÓRIO " + name[-1],
           font=13, text_color=col(0.7, 0.95, 1), solid=False, z=1)
    # porta de verdade, trancada até o alarme (o roteiro destranca); depois abre sozinha quando o jogador chega
    L.node(f"Porta{name}", inst="door", props={"position": v(door_x * T + 32, (door_y + 2) * T), "Size": v(64, 2 * T),
           "Color": col(0.4, 0.8, 1, 1), "StartsLocked": "true", "MessageWhenLocked": "true",
           "LockedMessage": '"Laboratório trancado."', "AutoOpen": "true",
           "LockedSprite": L.tex("porta_lab_trancada", "props/fase3"),
           "ClosedSprite": L.tex("porta_lab_fechada", "props/fase3"),
           "UnlockedSprite": L.tex("porta_lab_aberta", "props/fase3")})


laboratorio("Lab1", LAB1, 3, 58, -3.5, "esquerda")
laboratorio("Lab2", LAB2, 8, 44, 15, "direita")

# ---------- entrada: a escada (em cima) e o elevador desligado (embaixo), na parede da direita ----------
L.prop("ElevadorEntrada", ".", ENTRADA[2] * T + 40, 75 * T, 48, 3 * T, CONCRETE, depth=3 * T, solid=False,
       sprite="elevador_quebrado", flip=True)
L.node("AvisoElevadorEntrada", inst="item_slot", props={"position": v(ENTRADA[2] * T, 73.5 * T), "Size": v(140, 200),
       "RequiredItem": '"nunca"', "MissingMessage": '"ELEVADOR - fora de serviço. Pelo menos a escada chegou aqui."'})
L.prop("PortaEscada", ".", ENTRADA[2] * T + 40, 71 * T, 48, 3 * T, col(0.5, 0.42, 0.3), depth=3 * T, solid=False,
       sprite="porta_escada", flip=True)
L.prop("Placa4Andar", ".", 10 * T + 32, ENTRADA[1] * T + 24, 160, 26, col(0, 0, 0, 0), "4º ANDAR", font=14,
       text_color=col(0.95, 0.9, 0.6), solid=False, z=1)

# ---------- MVP 5: porta da área final, alarme, laboratórios seguros, computador e quadro de energia ----------
# porta de vidro no pé do corredor de vidro, que leva à área final (trancada até a senha certa)
L.node("PortaFinal", inst="door", props={"position": v(6 * T, (VIDRO[3] + 1) * T), "Size": v(2 * T, 48),
       "StartsLocked": "true", "MessageWhenLocked": "true",
       "LockedMessage": '"Porta de vidro trancada. Tem uma fechadura com senha do lado."',
       "LockedSprite": L.tex("porta_vidro_estreita_fechada", "props/fase3"),
       "UnlockedSprite": L.tex("porta_vidro_estreita_aberta", "props/fase3")})
# fechadura brilhando na parede da frente, logo à direita da porta (no alto do corredor), e onde apertar E
L.node("LuzFechadura", type="Node2D", props={"z_index": "2", "position": v((CORR[2] + 1) * T + 26, (CORR[1] + 1) * T - 6),
       "script": 'ExtResource("glow")', "Texture": L.tex("fechadura", "props/fase3"),
       "GlowColor": col(1, 0.3, 0.25), "GlowRadius": "46", "LightEnergy": "1.1"})
L.node("Fechadura", type="Area2D", props={"position": v(6.5 * T, (CORR[1] + 1) * T), "script": 'ExtResource("interactable")',
       "Size": v(5 * T, 2 * T), "Prompt": '"Digitar senha"', "PromptHeight": "110"})

# corredor de vidro: as paredes dos dois lados são janelas (a noite e a chuva lá fora)
for ty in range(VIDRO[1], VIDRO[3] + 1):
    for side, wx in (("E", VIDRO[0] - 1), ("D", VIDRO[2] + 1)):
        L.prop(f"Vidro{side}{ty}", ".", wx * T + 32, (ty + 1) * T, 64, 64, col(0, 0, 0, 0), solid=False,
               sprite="vidro_lateral", folder="props/fase3", z=1, flip=side == "D")

# câmeras nas paredes do corredor: apagadas até o jogador tentar a senha; aí apitam, piscam e chamam os fantasmas
CAMERAS = [("E", ty) for ty in (36, 50, 66)] + [("D", ty) for ty in (40, 55, 62)]
for i, (side, ty) in enumerate(CAMERAS):
    x = (CORR[0] * T - 18) if side == "E" else ((CORR[2] + 1) * T + 18)
    p = {"position": v(x, ty * T), "script": 'ExtResource("glow")', "Texture": L.tex("camera0", "props/fase3"),
         "Glow": "false", "GlowColor": col(1, 0.2, 0.15), "GlowRadius": "34", "LightEnergy": "0.8", "LightScale": "0.7"}
    if side == "D": p["scale"] = v(-1, 1)
    L.node(f"Camera{i+1}", type="Node2D", groups=["cameras"], props=p)

# alarme: o corredor (com o hall e a entrada) vira uma sala de sobrevivência sem fim, que o roteiro liga e desliga.
# Os fantasmas só surgem nos pontos Spawn (no corredor), nunca dentro dos laboratórios.
AL = (CORR[0], CORR[1])
L.node("Alarme", inst="room", props={"position": v(AL[0] * T, AL[1] * T), "Size": v(8 * T, (CORR[3] - CORR[1] + 1) * T),
       "Waves": "0", "SurviveTime": "100000", "SurviveSpawnInterval": "2.0", "SurviveMaxAlive": "6",
       "EnemySpeedScale": "0.95",
       "StartOnEnter": "false", "StartBanner": '"ALARME!"', "ClearBanner": '"ALARME DESLIGADO"',
       "RespawnPoint": 'NodePath("Volta")'}, node_paths=["RespawnPoint"])
L.node("Volta", "Alarme", type="Marker2D", props={"position": v((10 - AL[0]) * T, (47.5 - AL[1]) * T)})
spawns = [(6, ty) for ty in (31, 35, 39, 47, 55, 63, 71)] + [(10, 33), (10, 73)]
for i, (gx, gy) in enumerate(spawns):
    L.node(f"Spawn{i+1}", "Alarme", type="Marker2D", props={"position": v((gx - AL[0]) * T, (gy - AL[1]) * T)})

# laboratórios: áreas seguras (fantasma não entra) e o ponto onde o jogador volta se for reprovado
for name, r_ in (("Lab1", LAB1), ("Lab2", LAB2)):
    L.node(f"Seguro{name}", type="Node2D", props={"position": v(r_[0] * T, r_[1] * T), "script": 'ExtResource("safe_zone")',
           "Size": v((r_[2] - r_[0] + 1) * T, (r_[3] - r_[1] + 1) * T)})
L.node("VoltaLab1", type="Marker2D", props={"position": v(1.5 * T, 61 * T)})
L.node("VoltaLab2", type="Marker2D", props={"position": v(10 * T, 47.5 * T)})

# Laboratório 2: o computador piscando na bancada encostada na parede do fundo (longe da porta), perto do fim dela
PC_X = (LAB2[2] + 1) * T - 36
L.node("Monitor", type="Node2D", props={"z_index": "2", "position": v(PC_X, (LAB2[3] - 2) * T + 44),
       "script": 'ExtResource("glow")', "Texture": L.tex("monitor0", "props/fase3"),
       "BlinkTexture": L.tex("monitor1", "props/fase3"), "BlinkInterval": "0.45",
       "GlowColor": col(0.4, 0.85, 1), "GlowRadius": "52"})
L.node("Computador", type="Area2D", props={"position": v(PC_X - 70, (LAB2[3] - 1.2) * T), "script": 'ExtResource("interactable")',
       "Size": v(300, 240), "Prompt": '"Usar computador"', "PromptHeight": "150"})

# Laboratório 1: o quadro de energia na parede de cima, no canto do fundo (à esquerda da lousa, longe da porta)
L.node("Quadro", type="Node2D", props={"position": v((LAB1[0] + 0.5) * T, LAB1[1] * T + 4), "script": 'ExtResource("glow")',
       "Texture": L.tex("quadro_energia", "props/fase3"), "GlowColor": col(1, 0.75, 0.3), "GlowRadius": "60"})
L.node("QuadroEnergia", type="Area2D", props={"position": v((LAB1[0] + 1) * T, (LAB1[1] + 1) * T), "script": 'ExtResource("interactable")',
       "Size": v(3 * T, 2.5 * T), "Prompt": '"Consertar fiação"', "PromptHeight": "120"})
# ... e, na parede de baixo (lado esquerdo), o distribuidor de energia (desafio de calibrar)
L.node("Distribuidor", type="Node2D", props={"position": v((LAB1[0] + 1.5) * T, (LAB1[3] + 1) * T - 4),
       "script": 'ExtResource("glow")', "Texture": L.tex("distribuidor", "props/fase3"), "GlowColor": col(1, 0.75, 0.3),
       "GlowRadius": "60"})
L.node("CalibrarDistribuidor", type="Area2D", props={"position": v((LAB1[0] + 1.5) * T, LAB1[3] * T),
       "script": 'ExtResource("interactable")', "Size": v(3 * T, 2.5 * T), "Prompt": '"Calibrar distribuidor"',
       "PromptHeight": "130"})

# ---------- auditório (planta NOVA_AREA_FINAL): plateia virada para o palco, embaixo ----------
# Área restrita: por fora não se vê nada (CoberturaAuditorio). A porta fica trancada até o jogador apertar E nela;
# a cobertura some quando ele entra (o roteiro cuida disso).
AUD_CX = (AUD[0] + AUD[2] + 1) * T / 2
AISLE = (AUD[0] + AUD[2]) // 2
L.prop("Palco", ".", AUD_CX, (PALCO[3] + 1) * T, (PALCO[2] - PALCO[0] + 1) * T, (PALCO[3] - PALCO[1] + 1) * T,
       col(0.45, 0.3, 0.18), solid=False, z=-4, sprite="palco", folder="props/fase3")
L.prop("Pulpito", ".", AUD_CX + 2 * T, (PALCO[1] + 1) * T, 48, 56, WOOD, depth=28, sprite="pulpito", folder="props/fase3")
# o diploma num suporte no meio do palco e as duas escadinhas nas laterais (como na cena em 3D)
L.prop("DiplomaPalco", ".", AUD_CX, (PALCO[1] + 1) * T - 8, 48, 40, col(0, 0, 0, 0), solid=False, sprite="diploma_palco",
       folder="props/fase3")
for side, tx in (("E", AUD[0]), ("D", AUD[2])):
    L.prop(f"EscadaPalco{side}", ".", tx * T + 32, PALCO[1] * T + 24, 64, 64, col(0, 0, 0, 0), solid=False, z=-3,
           sprite="escada_palco", folder="props/fase3")
# piso do auditório: madeira marrom clara (mais clara que as poltronas), por baixo do tapete vermelho
L.node("PisoAuditorio", type="ColorRect", props={"z_index": "-6", "offset_left": f"{AUD[0] * T}.0",
       "offset_top": f"{AUD[1] * T}.0", "offset_right": f"{(AUD[2] + 1) * T}.0", "offset_bottom": f"{PALCO[1] * T}.0",
       "mouse_filter": "2", "color": col(0.66, 0.5, 0.34, 1)})
L.prop("Tapete", ".", AISLE * T + 32, PALCO[1] * T, 56, (PALCO[1] - AUD[1]) * T, col(0.42, 0.1, 0.12), solid=False,
       z=-5)
# fileiras de poltronas coladas umas nas outras (como as cadeiras pretas da parede), em dois blocos: um de cada lado
# do tapete, com corredores livres nas laterais
SEAT = 40
for row, ty in enumerate(range(AUD[1] + 1, AUD[3] - 3, 2)):
    for side, (x0, x1) in enumerate([(AUD[0] + 1, AISLE), (AISLE + 1, AUD[2])]):   # tiles [x0, x1)
        count = (x1 - x0) * T // SEAT
        start = (x0 + x1) * T / 2 - count * SEAT / 2 + SEAT / 2
        for k in range(count):
            L.prop(f"Poltrona{row+1}_{side}_{k}", ".", start + k * SEAT, ty * T + 56, SEAT, 46, col(0.45, 0.12, 0.14),
                   depth=22, sprite="poltrona", folder="props/fase3")
L.prop("PlacaAuditorio", ".", AUD_CX, AUD[1] * T - 40, 240, 26, col(0, 0, 0, 0), "AUDITÓRIO", font=13,
       text_color=col(1, 0.85, 0.6), solid=False, z=1)
LIG_H = LIGACAO[1]
L.prop("PlacaRestrita", ".", (LIG_H[0] + 1.5) * T, LIG_H[1] * T - 6, 220, 24, col(0, 0, 0, 0), "ÁREA RESTRITA", font=12,
       text_color=col(1, 0.4, 0.35), solid=False, z=1)
L.node("PortaAuditorio", inst="door", props={"position": v(LIG_H[2] * T + 32, (LIG_H[3] + 1) * T), "Size": v(64, 2 * T),
       "StartsLocked": "true", "AutoOpen": "true", "Color": col(0.4, 0.8, 1, 1),
       "LockedSprite": L.tex("porta_lab_trancada", "props/fase3"),
       "ClosedSprite": L.tex("porta_lab_fechada", "props/fase3"),
       "UnlockedSprite": L.tex("porta_lab_aberta", "props/fase3")})
L.node("AbrirAuditorio", type="Area2D", props={"position": v((LIG_H[2] - 0.5) * T, (LIG_H[1] + 1) * T),
       "script": 'ExtResource("interactable")', "Size": v(2.5 * T, 2.5 * T), "Prompt": '"Abrir a porta"',
       "PromptHeight": "120"})
L.node("EntradaAuditorio", inst="area_trigger", props={"position": v((AUD[0] + 1) * T, AUD[1] * T),
       "Size": v((AUD[2] - AUD[0]) * T, (PALCO[3] - AUD[1] + 1) * T), "Color": col(0, 0, 0, 0)})
L.node("CoberturaAuditorio", type="ColorRect", props={"z_index": "20", "offset_left": f"{AUD[0] * T}.0",
       "offset_top": f"{AUD[1] * T - 8}.0", "offset_right": f"{(AUD[2] + 1) * T}.0",
       "offset_bottom": f"{(PALCO[3] + 1) * T}.0", "mouse_filter": "2", "color": col(0.03, 0.035, 0.045, 1)})

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


for i, (ty, mode) in enumerate([(30, FLICKER), (36, BROKEN), (42, STEADY), (48, FLICKER), (54, BROKEN), (60, FLICKER),
                                (66, STEADY), (72, FLICKER)]):
    light(f"Corredor{i+1}", 6 * T, ty * T, TUBO, 2.0, 0.8, mode)
for i, (tx, ty, mode) in enumerate([(5, 4, STEADY), (19, 4, BROKEN), (12, 13, FLICKER), (21, 13, STEADY),
                                    (27, 12, FLICKER), (20, 19, BROKEN), (12, 22, FLICKER), (3, 23, STEADY)]):
    light(f"Convivencia{i+1}", tx * T, (ty + CY) * T, TUBO, 2.2, 0.85, mode)
light("Vidro", 6 * T, (VIDRO[1] + 2.5) * T, TUBO, 1.4, 0.7, FLICKER)
for i, ty in enumerate([AUD[1] + 3, AUD[3] - 1]):
    light(f"Auditorio{i+1}", (AUD[0] + AUD[2] + 1) * T / 2, ty * T, col(1, 0.85, 0.6), 2.2, 0.8, BROKEN if i else FLICKER)
light("Hall", 10 * T + 32, 33 * T, TUBO, 1.6, 0.8, BROKEN)
for name, r in (("Lab1", LAB1), ("Lab2", LAB2)):
    for i, ty in enumerate([r[1] + 3, r[3] - 3]):
        light(f"{name}_{i+1}", (r[0] + r[2] + 1) * T / 2, ty * T, col(0.6, 0.85, 1), 2.0, 0.8, FLICKER if i else BROKEN)
light("Entrada", 10 * T + 32, 72 * T, TUBO, 1.8, 0.8, STEADY)
light("PlacaEscada", ENTRADA[2] * T + 16, 69 * T, col(0.2, 1, 0.45), 0.8, 0.8, STEADY, tube=False)
light("ElevadorEntrada", ENTRADA[2] * T + 16, 73 * T, col(1, 0.15, 0.1), 1.2, 0.9, PULSE, tube=False)

# ---------- jogador / interface ----------
L.node("Player", inst="player", props={"position": v(10 * T + 32, 69 * T + 32), "StartWithAllWeapons": "false"})
L.node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
L.node("MessageScreen", inst="message_screen")
L.node("ItemInspect", inst="item_inspect")

paths = {"Player": "Player", "Hud": "Hud", "Screen": "MessageScreen", "Lab1": "Lab1", "Lab2": "Lab2",
         "Convivencia": "Convivencia", "Alarme": "Alarme", "PortaFinal": "PortaFinal", "Fechadura": "Fechadura",
         "LuzFechadura": "LuzFechadura", "Computador": "Computador", "Monitor": "Monitor",
         "QuadroEnergia": "QuadroEnergia", "Quadro": "Quadro", "VoltaLab1": "VoltaLab1", "VoltaLab2": "VoltaLab2",
         "PortaLab1": "PortaLab1", "PortaLab2": "PortaLab2", "PortaAuditorio": "PortaAuditorio",
         "AbrirAuditorio": "AbrirAuditorio", "EntradaAuditorio": "EntradaAuditorio",
         "CoberturaAuditorio": "CoberturaAuditorio", "CalibrarDistribuidor": "CalibrarDistribuidor",
         "Distribuidor": "Distribuidor"}
L.write(sys.argv[1], root_node("Fase3", "fase3", paths))
print("chão", len(L.floor), "paredes dentro", len(L.inner_walls), "fora", len(L.outer_walls))
