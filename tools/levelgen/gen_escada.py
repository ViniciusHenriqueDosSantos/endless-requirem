# Gera levels/escada.tscn: a escada do prédio (fim da Fase 2), vista de cima, do térreo (0) ao 4º andar.
# É o vão da escada: duas colunas de lances lado a lado (A e B), com uma parede no meio.
# O jogador sobe um lance; no alto, a câmera gira 180° e ele aparece no pé do próximo lance, na outra coluna.
# A coluna do lado mostra o lance de onde ele saiu, desabando. 8 lances = 2 por andar. Sem fantasmas.
# Os obstáculos de cada lance (barricada, degrau quebrado, lance pela metade, lâmpada, extintor) são criados
# pelo roteiro (levels/Escada.cs, tabela Lances), porque cada coluna muda de lance a cada giro.
# Uso: python3 tools/levelgen/gen_escada.py levels/escada.tscn
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from lib import Level, T, v, col, px, root_node, CONCRETE

L = Level()
TOP, FOOT = 3, 17              # linhas dos lances (de cima para baixo)
A, B = (0, 2), (4, 6)          # colunas dos lances
L.add_floor(A[0], TOP, A[1], FOOT)            # lance da coluna A
L.add_floor(B[0], TOP, B[1], FOOT)            # lance da coluna B
L.add_floor(A[0], FOOT + 1, A[1], FOOT + 3)   # patamar do térreo (começo), embaixo da coluna A
L.add_floor(B[0], TOP - 1, B[1], TOP - 1)     # porta do 4º andar (no alto da coluna B)
EXIT = (B[0], TOP - 4, B[1], TOP - 2)         # patamar do 4º andar, atrás da porta
L.add_floor(*EXIT)
L.build_walls()

L.res('Script', 'res://levels/Escada.cs', 'escada')
for name, path in [('player', 'player/player'), ('hud', 'hud/hud'), ('message_screen', 'message_screen/message_screen'),
                   ('area_trigger', 'area_trigger/area_trigger'), ('door', 'door/door')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
for name, path in [('night_overlay', 'night/night_overlay'), ('flicker_light', 'night/flicker_light'),
                   ('rain', 'weather/rain')]:
    L.res('PackedScene', f'res://objects/{path}.tscn', name)
L.res('Texture2D', 'res://sprites/lights/tubo.png', 'light_tubo')
L.res('Script', 'res://objects/escada/CollapseFloor.cs', 'collapse')
L.res('AudioStream', 'res://sounds/escada/desabamento.wav', 'som_desabamento')

# ---------- arte: degraus com ranhuras nos lances, granito nos patamares, parede interna ----------
from tiling import h
floor_mat = {c: ("rampa" if TOP <= c[1] <= FOOT else "granito") for c in L.floor}


def deco_rule(c, style, below):
    r = h("decoE", *c)
    if r < 0.10: return "saida"
    if r < 0.28: return "rachada"
    if r < 0.48: return "mofo"
    return None


DECALS = [(lambda m: True, [("rachadura", 3, 0.08), ("poca", 2, 0.04), ("papel", 2, 0.03), ("mancha", 2, 0.03)])]
L.tiled_layers("fase1", floor_mat, deco_rule, style_rule=lambda c: "interno", decal_rules=DECALS)
L.node("Fundo", type="ColorRect", props={"z_index": "-12", "offset_left": f"{-14 * T}.0", "offset_top": f"{-14 * T}.0",
       "offset_right": f"{21 * T}.0", "offset_bottom": f"{35 * T}.0", "mouse_filter": "2",
       "color": col(0.03, 0.035, 0.045, 1)})

# número do andar pintado no chão (o roteiro troca a figura e a coluna a cada andar) e o "4" no patamar final
L.prop("NumeroAndar", ".", 1.5 * T, (FOOT + 1) * T - 12, 96, 112, col(0, 0, 0, 0), solid=False, sprite="andar0",
       folder="props/escada", z=-7)
L.prop("Andar4", ".", 5.5 * T, (EXIT[3] + 1) * T - 8, 96, 112, col(0, 0, 0, 0), solid=False, sprite="andar4",
       folder="props/escada", z=-7)

# ---------- o que desaba, os obstáculos (criados pelo roteiro), começo e saída ----------
L.node("Desabamento", type="Node2D", props={"z_index": "-6", "script": 'ExtResource("collapse")'})
L.node("Obstaculos", type="Node2D", props={"y_sort_enabled": "true"})
L.node("Inicio", inst="area_trigger", props={"position": v(A[0] * T, FOOT * T), "Size": v(3 * T, T),
       "Color": col(0, 0, 0, 0)})
L.prop("PortaEscadaEntrada", ".", (A[1] + 1) * T - 24, (FOOT + 4) * T, 48, 3 * T, col(0.5, 0.42, 0.3), depth=3 * T,
       solid=False, sprite="porta_escada", flip=True)
L.node("PortaSaida", inst="door", props={"position": v(5.5 * T, TOP * T), "Size": v(3 * T, 64), "StartsLocked": "true",
       "WallStyle": "true", "Color": col(0.9, 0.75, 0.3, 1)})
L.node("CoberturaSaida", type="ColorRect", props={"z_index": "20", "offset_left": f"{(EXIT[0] - 1) * T}.0",
       "offset_top": f"{(EXIT[1] - 1) * T}.0", "offset_right": f"{(EXIT[2] + 2) * T}.0",
       "offset_bottom": f"{(TOP - 1) * T}.0", "mouse_filter": "2", "color": col(0.03, 0.035, 0.045, 1)})
L.node("Saida", inst="area_trigger", props={"position": v(EXIT[0] * T, EXIT[1] * T),
       "Size": v((EXIT[2] - EXIT[0] + 1) * T, (EXIT[3] - EXIT[1] + 1) * T), "Text": '"4º ANDAR"'})

# ---------- noite: luzes de emergência ao longo dos lances e a chuva lá fora ----------
L.node("Noite", type="CanvasModulate", groups=["night"], props={"color": col(0.11, 0.12, 0.2, 1)})
L.node("NightOverlay", inst="night_overlay")
L.node("Chuva", inst="rain", props={"AlwaysInside": "true", "LightningInterval": v(12, 26)})
L.node("Luzes", type="Node2D")
n = 0
for cx in (1.5, 5.5):
    for ty in (5, 10, 15):
        n += 1
        L.node(f"Emergencia{n}", "Luzes", inst="flicker_light", props={
            "position": v(cx * T, ty * T), "color": col(1, 0.82, 0.55), "texture_scale": "1.9", "BaseEnergy": "0.8",
            "Behavior": str(1 if n % 2 else 2), "Seed": str(500 + n), "texture": 'ExtResource("light_tubo")'})
L.node("Terreo", "Luzes", inst="flicker_light", props={"position": v(1.5 * T, 19.5 * T), "color": col(1, 0.82, 0.55),
       "texture_scale": "1.6", "BaseEnergy": "0.8", "Behavior": "0", "Seed": "520", "texture": 'ExtResource("light_tubo")'})
L.node("LuzSaida", "Luzes", inst="flicker_light", props={"position": v(5.5 * T, (TOP - 2) * T),
       "color": col(0.2, 1, 0.45), "texture_scale": "1.2", "BaseEnergy": "0.8", "Behavior": "0", "Seed": "540"})

L.node("Player", inst="player", props={"position": v(1.5 * T, 19.5 * T + 16), "StartWithAllWeapons": "false"})
L.node("Hud", inst="hud", props={"Player": 'NodePath("../Player")'}, node_paths=["Player"])
L.node("MessageScreen", inst="message_screen")

paths = {"Player": "Player", "Hud": "Hud", "Screen": "MessageScreen", "PortaSaida": "PortaSaida", "Saida": "Saida",
         "Inicio": "Inicio", "CoberturaSaida": "CoberturaSaida", "Desabamento": "Desabamento", "Obstaculos": "Obstaculos",
         "NumeroAndar": "NumeroAndar"}
root = root_node("Escada", "escada", paths)
root += f"\nColunaA = {1.5 * T:g}\nColunaB = {5.5 * T:g}\nFaixa = {3 * T:g}\nPeDoLance = {(FOOT + 1) * T:g}"
root += f"\nAltoDoLance = {TOP * T:g}\nFundoDoTerreo = {(FOOT + 4) * T:g}"
root += '\nSomDesabamento = ExtResource("som_desabamento")'
L.write(sys.argv[1], root)
print("chão", len(L.floor), "lance", FOOT - TOP + 1, "tiles")
