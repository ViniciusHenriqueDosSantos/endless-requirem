# Tileset da Fase 1 (UNIFESP SJC): pisos, paredes e sombras.
# Saída: sprites/tiles/fase1_tiles.png + tilesets/fase1_tileset.tres + tilesets/fase1_tileset_outer.tres
#        + tools/levelgen/tiles_fase1.json (onde cada peça está no atlas, lido pelo gerador da fase).
#
# Atlas com 16 colunas de tiles de 16 px. Linhas:
#   0  compatibilidade (granito A/B, topo e frente de parede genéricos)
#   1  bloquete vermelho em espinha de peixe   2 calçada cimentada   3 asfalto
#   4  piso sextavado   5 granito da praça   6 granito branco polido   7 cerâmica do RU   8 aço / grama
#   9..11  topo de parede (16 combinações de vizinhos) nos estilos interno, fachada e externo
#   12..14 frente de parede nos mesmos estilos (4 combinações + variações decoradas)
#   15 sombras de contato no chão (camada por cima do piso)
import json, os
from pix import Canvas, ROOT, rng, mix, shade, hexc
from palette import C

T = 16
COLS = 16
atlas = Canvas(T * COLS, T * 22)
names = {}


def tile(name, col, row):
    cv = Canvas(T, T)
    names[name] = [col, row]
    return cv


def place(cv, name):
    col, row = names[name]
    atlas.blit(cv, col * T, row * T)


# ---------------------------------------------------------------- pisos
def herringbone_owner(x, y):
    """Espinha de peixe 90° com tijolos de 8x4 px. O desenho volta ao começo a cada 32 px:
    os 4 tiles bloquete0..3 formam um bloco 2x2 (o gerador escolhe pela posição do tile)."""
    u = 4
    ux, uy = (x // u) % 8, (y // u) % 8
    for a in range(-4, 5):
        for b in range(-4, 5):
            ox, oy = a + 2 * b, a - 2 * b
            for k, cells in enumerate(([(0, 0), (1, 0)], [(0, 1), (0, 2)])):
                for cx, cy in cells:
                    if (ox + cx) % 8 == ux and (oy + cy) % 8 == uy:
                        return (ox % 8, oy % 8, k)
    return None


_HB = {}


def floor_bricks(variant, tactile=False, curb=False):
    cv = Canvas(T, T)
    ox, oy = (variant % 2) * T, (variant // 2) * T
    r = rng(100 + variant)
    for y in range(T):
        for x in range(T):
            gx, gy = ox + x, oy + y
            key = (gx % 32, gy % 32)
            if key not in _HB:
                _HB[key] = herringbone_owner(*key)
            b = _HB[key]
            tr = rng(hash(b) & 0xffff).random()
            c = C["brick"] if tr < 0.55 else (C["brick_l"] if tr < 0.8 else C["brick_d"])
            right = _HB.setdefault(((gx + 1) % 32, gy % 32), herringbone_owner((gx + 1) % 32, gy % 32))
            down = _HB.setdefault((gx % 32, (gy + 1) % 32), herringbone_owner(gx % 32, (gy + 1) % 32))
            left = _HB.setdefault(((gx - 1) % 32, gy % 32), herringbone_owner((gx - 1) % 32, gy % 32))
            up = _HB.setdefault((gx % 32, (gy - 1) % 32), herringbone_owner(gx % 32, (gy - 1) % 32))
            if right != b or down != b:
                c = C["brick_grout"]
            elif left != b or up != b:
                c = mix(c, C["brick_hi"], 0.45)
            cv.put(x, y, c)
    for _ in range(3):
        cv.put(r.randrange(T), r.randrange(T), C["brick_d"])
    if tactile:  # faixa de piso tátil amarelo
        cv.rect(0, 5, T, 6, hexc("d9b23a"))
        for x in range(1, T, 3):
            for yy in (6, 8):
                cv.put(x, yy, hexc("f0d062")); cv.put(x, yy + 1, hexc("b8932a"))
        cv.hline(0, 5, T, hexc("f0d062")); cv.hline(0, 10, T, hexc("8f7024"))
    if curb:  # meio-fio pintado de branco na beirada de baixo
        cv.rect(0, 10, T, 4, C["curb"]); cv.hline(0, 10, T, hexc("f4f3ee"))
        cv.rect(0, 14, T, 2, C["curb_d"]); cv.hline(0, 9, T, C["brick_grout"])
        for x in r.sample(range(T), 3): cv.put(x, 12, hexc("b7b5ae"))
    return cv


def floor_walk(variant, curb=False):
    cv = Canvas(T, T, C["walk"])
    r = rng(200 + variant)
    cv.noise(0, 0, T, T, [C["walk_l"], C["walk_d"]], 0.18, r)
    cv.hline(0, 0, T, C["walk_joint"])
    if variant % 2: cv.vline(0, 0, T, C["walk_joint"])
    if variant == 3:
        cv.line(4, 6, 9, 8, C["walk_d"])
    if curb:
        cv.rect(0, 11, T, 3, C["curb"]); cv.hline(0, 11, T, hexc("f4f3ee")); cv.rect(0, 14, T, 2, C["curb_d"])
    return cv


def floor_asphalt(variant, line=False, under_curb=False):
    cv = Canvas(T, T, C["asph"])
    r = rng(300 + variant)
    cv.noise(0, 0, T, T, [C["asph_l"], C["asph_d"], C["asph_d"]], 0.35, r)
    if variant == 2:
        for p in [(3, 9), (4, 9), (4, 10), (5, 10), (11, 4)]: cv.put(*p, hexc("45474b"))
    if variant == 3:  # remendo
        cv.rect(8, 9, 6, 4, hexc("535559")); cv.noise(8, 9, 6, 4, [C["asph_l"]], 0.2, r)
    if line:  # faixa tracejada branca
        cv.rect(2, 7, 10, 2, C["paint"]); cv.noise(2, 7, 10, 2, [hexc("c9c7bf")], 0.2, r)
    if under_curb:
        cv.rect(0, 0, T, 2, hexc("000000", 60))
    return cv


def floor_hex(variant):
    cv = Canvas(T, T)
    r = rng(400 + variant)
    centers = [(0, 0), (8, 0), (4, 8), (12, 8), (16, 0), (0, 16), (16, 16), (8, 16), (4, -8), (12, -8),
               (-4, 8), (20, 8), (4, 24), (12, 24)]

    def cell(x, y):
        best, bi = 1e9, 0
        for i, (cx, cy) in enumerate(centers):
            d = (x - cx) ** 2 + ((y - cy) * 1.15) ** 2
            if d < best: best, bi = d, i
        cx, cy = centers[bi]
        return (cx % 16, cy % 16)

    tones = {}
    for y in range(T):
        for x in range(T):
            k = cell(x + 0.5, y + 0.5)
            tones.setdefault(k, r.choice([C["hex"], C["hex"], C["hex_l"], mix(C["hex"], C["hex_d"], 0.3)]))
            edge = cell(x + 1.5, y + 0.5) != k or cell(x + 0.5, y + 1.5) != k
            if edge:
                c = C["grass_d"] if r.random() < (0.22 if variant != 0 else 0.05) else C["hex_d"]
            else:
                c = tones[k]
            cv.put(x, y, c)
    cv.noise(0, 0, T, T, [C["hex_l"]], 0.04, r)
    return cv


def floor_plaza(variant):
    base = [C["plaza"], mix(C["plaza"], C["plaza_l"], 0.4), mix(C["plaza"], C["plaza_d"], 0.25), C["plaza"]][variant]
    cv = Canvas(T, T, base)
    r = rng(500 + variant)
    cv.noise(0, 0, T, T, [C["plaza_l"], C["plaza_d"]], 0.12, r)
    cv.hline(0, 0, T, mix(base, C["plaza_joint"], 0.6)); cv.vline(0, 0, T, mix(base, C["plaza_joint"], 0.6))
    cv.hline(1, 1, T - 1, mix(base, C["plaza_l"], 0.5))
    if variant == 3:  # mancha de água / folha seca
        cv.put(10, 11, C["plaza_d"]); cv.put(11, 11, C["plaza_d"]); cv.put(11, 12, C["plaza_d"])
    return cv


def floor_granite(variant):
    cv = Canvas(T, T, C["gran"])
    r = rng(600 + variant)
    # manchas suaves (o granito branco tem veios cinza claros) e poucos grãos escuros
    for _ in range(3):
        x, y = r.randrange(T), r.randrange(T)
        for k in range(r.randrange(3, 6)):
            cv.put((x + k) % T, (y + r.choice((0, 0, 1))) % T, C["gran_m"])
    cv.noise(0, 0, T, T, [C["gran_l"]], 0.10, r)
    cv.noise(0, 0, T, T, [C["gran_m"]], 0.06, r)
    cv.noise(0, 0, T, T, [C["gran_d"]], 0.015, r)
    cv.hline(0, 0, T, C["gran_joint"]); cv.vline(0, 0, T, C["gran_joint"])
    if variant == 2:  # veio cinza mais comprido atravessando a placa
        cv.line(2, 11, 13, 7, C["gran_m"]); cv.line(3, 11, 9, 9, C["gran_d"])
    return cv


def floor_ceramic(variant):
    """Cerâmica bege do RU assentada na diagonal (losangos), como nas fotos."""
    cv = Canvas(T, T)
    r = rng(700 + variant)
    tones = {}
    for y in range(T):
        for x in range(T):
            u, v = (x + y) % 16, (x - y) % 16
            k = ((x + y) // 8, (x - y) // 8)
            if k not in tones:
                tones[k] = r.choice([C["cer"], C["cer"], mix(C["cer"], C["cer_l"], 0.35), mix(C["cer"], C["cer_d"], 0.3)])
            c = tones[k]
            if u % 8 == 0 or v % 8 == 0:
                c = mix(c, C["cer_grout"], 0.55)
            elif u % 8 == 1:
                c = mix(c, C["cer_l"], 0.4)
            cv.put(x, y, c)
    return cv


def floor_steel(variant):
    cv = Canvas(T, T, C["steel"])
    for y in range(0, T, 4):
        for x in range((y // 4) % 2 * 2, T, 4):
            cv.put(x, y + 1, C["steel_l"]); cv.put(x + 1, y + 2, C["steel_d"])
    cv.hline(0, 0, T, C["steel_d"]); cv.vline(0, 0, T, C["steel_d"])
    return cv


def roof(variant):
    """Laje de concreto dos prédios vista de cima (aparece no lugar do vazio entre as salas)."""
    cv = Canvas(T, T, hexc("a9a69e"))
    r = rng(850 + variant)
    cv.noise(0, 0, T, T, [hexc("b3b0a8"), hexc("9f9c94")], 0.22, r)
    cv.hline(0, 0, T, hexc("96938b")); cv.vline(0, 0, T, hexc("96938b"))
    if variant == 2: cv.rect(5, 6, 4, 3, hexc("8f8c85"))  # ralo / mancha
    return cv


def floor_grass(variant):
    cv = Canvas(T, T, C["grass"])
    r = rng(800 + variant)
    cv.noise(0, 0, T, T, [C["grass_l"], C["grass_d"]], 0.3, r)
    for _ in range(5):
        x, y = r.randrange(T), r.randrange(1, T)
        cv.put(x, y, C["grass_l"]); cv.put(x, y - 1, C["grass_l"])
    return cv


# ---------------------------------------------------------------- abandono
EXTERIOR = {"bloquete", "calcada", "asfalto", "sextavado", "praca", "grama", "laje"}


def age(cv, mat, seed):
    """Décadas sem manutenção: manchas escuras, uma rachadinha e (do lado de fora) mato nas juntas."""
    r = rng(5000 + seed)
    for _ in range(2):
        x, y = r.randrange(T), r.randrange(T)
        cv.ellipse(x, y, r.uniform(1.5, 3.5), r.uniform(1, 2.5), hexc("1e1a14", 22))
    if r.random() < 0.5:
        x, y = r.randrange(2, 14), r.randrange(2, 14)
        for _ in range(r.randrange(3, 6)):
            cv.put(x, y, hexc("2a2620", 90)); x += r.choice((-1, 0, 1)); y += 1
    if mat in EXTERIOR:
        for _ in range(r.randrange(1, 4)):
            x, y = r.randrange(T), r.randrange(1, T)
            cv.put(x, y, C["grass_d"]); cv.put(x, y - 1, C["grass"])
    return cv


# ---------------------------------------------------------------- paredes
STYLES = {
    "interno": dict(cap=C["cap"], cap_l=C["cap_l"], cap_d=C["cap_d"]),
    "fachada": dict(cap=C["conc_d"], cap_l=C["conc"], cap_d=C["conc_k"]),
    "externo": dict(cap=C["leaf"], cap_l=C["leaf_l"], cap_d=C["leaf_d"]),
}


def wall_top(style, mask):
    s = STYLES[style]
    cv = Canvas(T, T, s["cap"])
    r = rng(900 + mask)
    n, e, so, w = mask & 1, mask & 2, mask & 4, mask & 8
    if style == "externo":
        cv.noise(0, 0, T, T, [C["leaf_l"], C["leaf_d"], C["grass_k"]], 0.4, r)
    elif style == "fachada":
        cv.noise(0, 0, T, T, [C["conc_stain"], C["conc_k"]], 0.12, r)
        # laje com brita e rufo: lembra o telhado plano visto de cima
        cv.noise(2, 2, 12, 12, [C["conc_l"]], 0.1, r)
    else:
        cv.noise(0, 0, T, T, [C["cap_l"]], 0.08, r)
    d, l = s["cap_d"], s["cap_l"]
    if not n: cv.hline(0, 0, T, d); cv.hline(0, 1, T, l)
    if not so: cv.hline(0, T - 1, T, d)
    if not w: cv.vline(0, 0, T, d); cv.vline(1, 1 if not n else 0, T - 1, l)
    if not e: cv.vline(T - 1, 0, T, d)
    return cv


def wall_front(style, ew, deco=None, seed=0):
    """Frente da parede (o lado que aparece em 3/4). ew: bit1 = parede à direita, bit2 = à esquerda."""
    r = rng(1000 + seed)
    if style == "interno":
        cv = Canvas(T, T, C["wall"])
        cv.rect(0, 0, T, 3, C["cap"]); cv.hline(0, 0, T, C["cap_d"]); cv.hline(0, 2, T, C["cap_d"])
        cv.hline(0, 3, T, C["wall_d"])
        cv.rect(0, 13, T, 3, C["base"]); cv.hline(0, 13, T, C["wall_d"])
        cv.noise(0, 4, T, 9, [C["wall_m"]], 0.10, r)
        for x in range(T):  # mofo subindo do rodapé
            for y in range(9, 13):
                if r.random() < (y - 8) * 0.12: cv.put(x, y, hexc("6f7a63", 70))
        if deco == "janela":  # janelão de vidro do RU e da cantina
            cv.rect(1, 4, 14, 8, C["frame"])
            cv.rect(2, 5, 12, 6, C["glass_l"]); cv.rect(2, 8, 12, 3, mix(C["glass_l"], C["leaf"], 0.5))
            cv.vline(8, 5, 6, C["frame"]); cv.line(3, 5, 5, 7, C["glass_hi"]); cv.line(10, 5, 11, 6, C["glass_hi"])
        elif deco == "extintor":
            cv.rect(10, 6, 3, 6, hexc("c62f26")); cv.put(11, 5, C["black"]); cv.put(11, 7, hexc("e6574a"))
            cv.rect(9, 4, 5, 1, hexc("c62f26")); text_mini(cv, 9, 4)
        elif deco == "saida":  # placa verde de SAÍDA
            cv.rect(4, 5, 8, 4, hexc("1f9a52")); cv.rect(5, 6, 2, 2, hexc("e9fff0")); cv.line(8, 7, 10, 7, hexc("e9fff0"))
        elif deco == "mural":  # quadro de avisos verde com papéis
            cv.rect(2, 5, 12, 7, hexc("2f6b45")); cv.frame(2, 5, 12, 7, C["wood_d"])
            for (x, y, c) in [(3, 6, "ffffff"), (6, 7, "f2d24b"), (9, 6, "f08ca0"), (11, 8, "ffffff"), (4, 9, "8fd0ff")]:
                cv.rect(x, y, 2, 2, hexc(c))
        elif deco in ("vidro", "vidro_quebrado"):  # divisória de vidro com caixilho de alumínio (entrada do RU)
            cv.rect(0, 3, T, 11, hexc("aeb5bc"))
            cv.rect(1, 4, 14, 9, hexc("1f2b38")); cv.vline(8, 4, 9, hexc("aeb5bc")); cv.hline(1, 9, 14, hexc("aeb5bc"))
            cv.line(2, 4, 5, 8, hexc("5c7690")); cv.line(10, 4, 12, 7, hexc("5c7690"))
            if deco == "vidro_quebrado":
                cv.rect(9, 4, 6, 5, hexc("0c1016"))
                for (x, y) in [(9, 4), (10, 5), (12, 4), (13, 6), (11, 7), (14, 5)]: cv.put(x, y, hexc("9fb6cc"))
        elif deco == "janela_quebrada":
            cv.rect(1, 4, 14, 8, C["frame"]); cv.rect(2, 5, 12, 6, hexc("0c1016"))
            for (x0, y0, x1, y1) in [(2, 5, 5, 8), (13, 5, 10, 9), (6, 10, 8, 7)]: cv.line(x0, y0, x1, y1, hexc("8fa9bf"))
            cv.vline(8, 5, 6, C["frame"])
        elif deco == "rachada":
            x, y = 9, 3
            while y < 13:
                cv.put(x, y, hexc("8d8f8f")); cv.put(x + 1, y, hexc("bfc2c4")); x += r.choice((-1, 0, 1)); y += 1
        elif deco == "mofo":
            for _ in range(40):
                x, y = r.randrange(T), r.randrange(5, 13)
                cv.put(x, y, hexc("4e5a44", 120))
        elif deco and deco.startswith("pichacao"):
            word = {"pichacao0": "2061", "pichacao1": "SOS", "pichacao2": "FUJA"}[deco]
            col = {"pichacao0": hexc("b3262e"), "pichacao1": hexc("2a2a2e"), "pichacao2": hexc("b3262e")}[deco]
            from pix import text
            text(cv, word, 1 if len(word) == 4 else 3, 6, col, spacing=0 if len(word) == 4 else 1)
            for x in r.sample(range(2, 14), 3): cv.vline(x, 11, 2, col)  # tinta escorrendo
        elif deco == "tv":  # TV de parede do RU, apagada e trincada
            cv.rect(3, 4, 10, 7, hexc("141519")); cv.rect(4, 5, 8, 5, hexc("1e232b")); cv.line(5, 5, 9, 9, hexc("5d6b7a"))
            cv.vline(8, 11, 2, hexc("33353b"))
        elif deco == "cartaz":  # cartaz amarelo do cardápio, rasgado
            cv.rect(4, 4, 7, 9, hexc("e3c23a")); cv.rect(5, 6, 5, 2, hexc("b03a2e")); cv.hline(5, 9, 5, hexc("8a6d1c"))
            cv.put(10, 12, C["wall"]); cv.put(9, 12, C["wall"]); cv.put(10, 11, C["wall"])
        elif deco == "porta_dupla":  # porta dupla bege da cozinha
            cv.rect(1, 3, 14, 10, hexc("ddd3bd")); cv.frame(1, 3, 14, 10, hexc("9e957f")); cv.vline(8, 3, 10, hexc("9e957f"))
            cv.put(6, 8, hexc("6d6553")); cv.put(10, 8, hexc("6d6553"))
        elif deco == "cozinha":  # janela de passar prato, cozinha escura atrás
            cv.rect(1, 4, 14, 6, hexc("0f1013")); cv.rect(1, 10, 14, 2, C["inox"]); cv.hline(1, 10, 14, C["inox_l"])
            cv.rect(3, 4, 9, 2, hexc("5a5f66"))  # coifa
            cv.put(5, 8, hexc("6a6f76")); cv.put(9, 7, hexc("6a6f76"))
    elif style == "fachada":
        cv = Canvas(T, T, C["conc"])
        cv.noise(0, 0, T, T, [C["conc_stain"], C["conc_l"]], 0.14, r)
        for x in r.sample(range(T), 4):  # escorridos escuros de décadas de chuva
            cv.vline(x, 2, r.randrange(3, 12), hexc("7d7462", 90))
        cv.rect(0, 0, T, 2, C["conc_d"]); cv.hline(0, 0, T, C["conc_k"])
        cv.rect(0, 14, T, 2, C["conc_d"]); cv.hline(0, 15, T, C["conc_k"])
        if deco in (None, "janela", "ar"):
            # faixa de janelas escuras: a marca dos prédios do ICT
            cv.rect(0, 5, T, 5, C["frame"])
            for x0 in (1, 9):
                cv.rect(x0, 6, 6, 3, C["glass"]); cv.put(x0, 6, C["glass_l"]); cv.put(x0 + 1, 6, C["glass_hi"])
                cv.put(x0 + 4, 7, C["glass_l"])
            cv.hline(0, 10, T, C["conc_k"])
            if deco == "ar":
                cv.rect(10, 11, 5, 3, hexc("dadcd8")); cv.hline(10, 13, 5, hexc("9a9c98")); cv.put(12, 12, hexc("9a9c98"))
        elif deco == "lisa":
            for x in r.sample(range(T), 2): cv.vline(x, 3, r.randrange(3, 8), C["conc_stain"])
        elif deco == "quebrada":  # faixa de janelas com vidros quebrados
            cv.rect(0, 5, T, 5, C["frame"])
            for x0 in (1, 9):
                cv.rect(x0, 6, 6, 3, hexc("0b0e12")); cv.put(x0 + 1, 6, hexc("8fa9bf")); cv.put(x0 + 4, 8, hexc("8fa9bf"))
            cv.hline(0, 10, T, C["conc_k"])
        elif deco == "trepadeira":  # trepadeira subindo pela fachada
            cv.rect(0, 5, T, 5, C["frame"])
            for x0 in (1, 9): cv.rect(x0, 6, 6, 3, C["glass"])
            for x in range(T):
                h_ = int(6 + 5 * abs(((x * 7) % 11) / 11 - 0.5) * 2 + r.random() * 3)
                for y in range(T - h_, T):
                    if r.random() < 0.75: cv.put(x, y, r.choice([C["leaf"], C["leaf_d"], C["leaf_l"]]))
        elif deco == "pichacao":
            from pix import text
            cv.rect(0, 5, T, 5, C["frame"])
            for x0 in (1, 9): cv.rect(x0, 6, 6, 3, C["glass"])
            text(cv, "SOS", 2, 10, hexc("b3262e"))
    else:  # externo: cerca viva com o gradil verde na frente
        cv = Canvas(T, T, C["leaf"])
        cv.noise(0, 0, T, T, [C["leaf_l"], C["leaf_d"]], 0.45, r)
        for x in range(T):
            cv.put(x, 0, C["leaf_d"] if x % 3 else C["leaf_l"])
        cv.rect(0, 13, T, 3, C["grass_d"])
        cv.hline(0, 4, T, C["fence_d"]); cv.hline(0, 12, T, C["fence_d"])
        for x in range(1, T, 3): cv.vline(x, 4, 9, C["fence"]); cv.put(x, 4, C["fence_l"])
    # quinas: parede acaba à esquerda ou à direita
    if not (ew & 2): cv.vline(0, 0, T, shade(cv.get(0, 8), -0.25))
    if not (ew & 1): cv.vline(T - 1, 0, T, shade(cv.get(T - 1, 8), -0.3))
    return cv


def text_mini(cv, x, y):
    pass


# ---------------------------------------------------------------- sombras de contato
def shadow(top, left, right):
    cv = Canvas(T, T)
    if top:
        cv.rect(0, 0, T, 2, hexc("000000", 70)); cv.hline(0, 2, T, hexc("000000", 35))
    if left:
        cv.rect(0, 0, 3, T, hexc("000000", 55)); cv.vline(3, 0, T, hexc("000000", 25))
    if right:
        cv.vline(T - 1, 0, T, hexc("000000", 40))
    return cv


# ---------------------------------------------------------------- monta o atlas
FLOORS = {
    "bloquete": (1, lambda v: floor_bricks(v)),
    "calcada": (2, lambda v: floor_walk(v)),
    "asfalto": (3, lambda v: floor_asphalt(v)),
    "sextavado": (4, lambda v: floor_hex(v)),
    "praca": (5, lambda v: floor_plaza(v)),
    "granito": (6, lambda v: floor_granite(v)),
    "ceramica": (7, lambda v: floor_ceramic(v)),
}
for name, (row, fn) in FLOORS.items():
    for v in range(4):
        cv = tile(f"{name}{v}", v, row); place(age(fn(v), name, row * 10 + v), f"{name}{v}")
extra = [("bloquete_tatil0", 4, 1, floor_bricks(0, tactile=True)), ("bloquete_tatil1", 5, 1, floor_bricks(1, tactile=True)),
         ("bloquete_meiofio0", 6, 1, floor_bricks(2, curb=True)), ("bloquete_meiofio1", 7, 1, floor_bricks(3, curb=True)),
         ("calcada_meiofio", 4, 2, floor_walk(0, curb=True)), ("asfalto_faixa", 4, 3, floor_asphalt(0, line=True)),
         ("asfalto_meiofio", 5, 3, floor_asphalt(1, under_curb=True)),
         ("aco0", 0, 8, floor_steel(0)), ("aco1", 1, 8, floor_steel(1)), ("grama0", 2, 8, floor_grass(0)),
         ("grama1", 3, 8, floor_grass(1)),
         ("laje0", 4, 8, roof(0)), ("laje1", 5, 8, roof(1)), ("laje2", 6, 8, roof(2)), ("grama2", 7, 8, floor_grass(2))]
for n, c, r_, cv in extra:
    tile(n, c, r_); place(cv, n)

for i, style in enumerate(["interno", "fachada", "externo"]):
    for mask in range(16):
        tile(f"topo_{style}_{mask}", mask, 9 + i); place(wall_top(style, mask), f"topo_{style}_{mask}")
    for ew in range(4):
        tile(f"frente_{style}_{ew}", ew, 12 + i); place(wall_front(style, ew, seed=ew), f"frente_{style}_{ew}")
DECOS = {"interno": ["janela", "extintor", "saida", "mural", "vidro", "vidro_quebrado", "janela_quebrada", "rachada",
                    "mofo", "pichacao0", "pichacao1", "pichacao2"],
         "fachada": ["janela", "ar", "lisa", "quebrada", "trepadeira", "pichacao"], "externo": []}
for i, style in enumerate(["interno", "fachada", "externo"]):
    for j, d in enumerate(DECOS[style]):
        tile(f"frente_{style}_{d}", 4 + j, 12 + i); place(wall_front(style, 3, d, seed=50 + j), f"frente_{style}_{d}")
EXTRA_INTERNO = ["tv", "cartaz", "porta_dupla", "cozinha"]
for j, d in enumerate(EXTRA_INTERNO):
    tile(f"frente_interno_{d}", j, 16); place(wall_front("interno", 3, d, seed=80 + j), f"frente_interno_{d}")
DECOS["interno"] += EXTRA_INTERNO


# ---------------------------------------------------------------- detalhes de abandono (camada por cima do piso)
def decal(kind, v):
    cv = Canvas(T, T)
    r = rng(7000 + v + sum(map(ord, kind)))
    if kind == "rachadura":
        x, y = r.randrange(1, 6), r.randrange(1, 6)
        for _ in range(14):
            cv.put(x, y, hexc("17140f", 150)); cv.put(x + 1, y, hexc("ffffff", 25))
            x += r.choice((0, 1, 1)); y += r.choice((0, 1, 1))
            if r.random() < 0.2:
                bx, by = x, y
                for _ in range(4): bx += 1; by -= r.choice((0, 1)); cv.put(bx, by, hexc("17140f", 110))
    elif kind == "poca":  # poça d'água: reflete um pouco da luz
        cv.ellipse(8, 9, 6.5 - v, 3.5, hexc("2a3a4c", 120)); cv.ellipse(7, 8, 3, 1.2, hexc("8fb0cc", 70))
    elif kind == "folhas":
        for _ in range(7):
            x, y = r.randrange(1, 15), r.randrange(1, 15)
            c = r.choice([hexc("8a6a32"), hexc("6e5426"), hexc("a07a3c"), hexc("5c6b2e")])
            cv.put(x, y, c); cv.put(x + 1, y, c)
    elif kind == "mato":
        for _ in range(3 + v * 2):
            x, y = r.randrange(2, 14), r.randrange(4, 15)
            for k in range(r.randrange(2, 5)):
                cv.put(x + r.choice((-1, 0, 1)), y - k, r.choice([C["grass"], C["grass_l"], C["leaf_d"]]))
    elif kind == "papel":
        for _ in range(2 + v):
            x, y = r.randrange(0, 11), r.randrange(0, 12)
            cv.rect(x, y, 4, 3, hexc("d9d4c3")); cv.hline(x, y + 2, 4, hexc("a39d88")); cv.put(x + 1, y + 1, hexc("8f99ad"))
    elif kind == "vidro":
        for _ in range(9):
            cv.put(r.randrange(T), r.randrange(T), r.choice([hexc("c8dcec", 200), hexc("8fb0cc", 180)]))
    elif kind == "forro":  # placa do forro que caiu do teto
        cv.rect(2, 3, 11, 9, hexc("e9e8e2")); cv.frame(2, 3, 11, 9, hexc("b8b6ad")); cv.noise(3, 4, 9, 7, [hexc("cfcdc4")], 0.3, r)
        cv.line(4, 6, 9, 10, hexc("a8a69d"))
    elif kind == "mancha":
        cv.ellipse(8, 8, 6, 4, hexc("2b1f14", 60)); cv.ellipse(9, 9, 3, 2, hexc("2b1f14", 50))
    return cv


DECALS = [("rachadura", 3), ("poca", 2), ("folhas", 2), ("mato", 2), ("papel", 2), ("vidro", 1), ("forro", 1), ("mancha", 2)]
k = 0
for kind, n in DECALS:
    for v in range(n):
        tile(f"decal_{kind}{v}", k % 16, 17 + k // 16); place(decal(kind, v), f"decal_{kind}{v}")
        k += 1

# estacionamento: sextavado com as faixas brancas das vagas
for v, (vert, hor) in enumerate([(True, False), (False, True)]):
    cv = age(floor_hex(1), "sextavado", 99 + v)
    if vert: cv.rect(0, 0, 2, T, hexc("d8d6cc", 200))
    if hor: cv.rect(0, 0, T, 2, hexc("d8d6cc", 200))
    tile(f"sextavado_vaga{v}", 4 + v, 4); place(cv, f"sextavado_vaga{v}")

k = 0
for top in (0, 1):
    for left in (0, 1):
        for right in (0, 1):
            if top or left or right:
                tile(f"sombra_{top}{left}{right}", k, 15); place(shadow(top, left, right), f"sombra_{top}{left}{right}")
                k += 1

# ---------------------------------------------------------------- terraço elevado: mureta, rampa e o nível de baixo
RAIL, RAIL_HI, RAIL_D = hexc("9aa1a8"), hexc("d4d9dd"), hexc("5d636a")


def mureta_top(mask):
    """Mureta de concreto na borda do terraço com o guarda-corpo de tubo de aço por cima (vista de cima)."""
    cv = Canvas(T, T, hexc("c9c6bc"))
    r = rng(1500 + mask)
    cv.noise(0, 0, T, T, [hexc("b8b4a8"), hexc("d6d3ca"), hexc("7d8a62")], 0.14, r)
    n, e, so, w = mask & 1, mask & 2, mask & 4, mask & 8
    vertical = (n or so) and not (e or w)
    if vertical or not (e or w):
        cv.vline(7, 0, T, RAIL); cv.vline(6, 0, T, RAIL_HI); cv.vline(8, 0, T, RAIL_D)
        for y in (2, 10): cv.rect(5, y, 5, 2, RAIL_D)
    else:
        cv.hline(0, 7, T, RAIL); cv.hline(0, 6, T, RAIL_HI); cv.hline(0, 8, T, RAIL_D)
        for x in (2, 10): cv.rect(x, 5, 2, 5, RAIL_D)
    if not w: cv.vline(0, 0, T, hexc("8f8b80"))
    if not e: cv.vline(T - 1, 0, T, hexc("4a4740"))
    if not n: cv.hline(0, 0, T, hexc("8f8b80"))
    if not so: cv.hline(0, T - 1, T, hexc("4a4740"))
    return cv


def mureta_front(ew):
    """Mureta vista de frente (ponta do terraço): concreto baixo com o guarda-corpo em cima."""
    cv = Canvas(T, T)
    r = rng(1600 + ew)
    cv.rect(0, 7, T, 9, hexc("c9c6bc")); cv.hline(0, 7, T, hexc("e2dfd6")); cv.hline(0, 15, T, hexc("8f8b80"))
    cv.noise(0, 8, T, 7, [hexc("b8b4a8"), hexc("7d8a62")], 0.15, r)
    cv.hline(0, 1, T, RAIL_HI); cv.hline(0, 2, T, RAIL); cv.hline(0, 4, T, RAIL_D)
    for x in (2, 10): cv.rect(x, 1, 2, 7, RAIL_D); cv.put(x, 1, RAIL_HI)
    if not (ew & 2): cv.vline(0, 0, T, hexc("8f8b80"))
    if not (ew & 1): cv.vline(T - 1, 0, T, hexc("4a4740"))
    return cv


def rampa(v):
    """Rampa de concreto claro com as ranhuras antiderrapantes (sobe da portaria para o terraço)."""
    cv = age(Canvas(T, T, hexc("cfcbc0")), "calcada", 1700 + v)
    r = rng(1700 + v)
    cv.noise(0, 0, T, T, [hexc("bdb9ae"), hexc("dcd8ce")], 0.15, r)
    for y in range(1, T, 4):
        cv.hline(0, y, T, hexc("a8a498")); cv.hline(0, y + 1, T, hexc("e0ddd4"))
    return cv


def lowered(cv):
    """Nível de baixo (estacionamento visto do terraço): mais escuro e azulado."""
    px = cv.img.load()
    for y in range(T):
        for x in range(T):
            r_, g, b, a = px[x, y]
            px[x, y] = (int(r_ * 0.55), int(g * 0.57), int(b * 0.66), a)
    return cv


for mask in range(16):
    tile(f"topo_mureta_{mask}", mask, 18); place(mureta_top(mask), f"topo_mureta_{mask}")
for ew in range(4):
    tile(f"frente_mureta_{ew}", ew, 19); place(mureta_front(ew), f"frente_mureta_{ew}")
for v in range(2):
    tile(f"rampa{v}", 4 + v, 19); place(rampa(v), f"rampa{v}")
for j, (name, fn) in enumerate([("bloquete0", lambda: floor_bricks(0)), ("bloquete1", lambda: floor_bricks(1)),
                                ("bloquete2", lambda: floor_bricks(2)), ("bloquete3", lambda: floor_bricks(3)),
                                ("sextavado0", lambda: floor_hex(0)), ("sextavado1", lambda: floor_hex(2)),
                                ("sextavado_vaga0", None)]):
    if fn is None:
        cv = age(floor_hex(1), "sextavado", 120); cv.rect(0, 0, 2, T, hexc("d8d6cc", 200))
    else:
        cv = age(fn(), "sextavado", 130 + j)
    tile(f"baixo_{name}", 6 + j, 19); place(lowered(cv), f"baixo_{name}")
DECOS["mureta"] = []


# ---------------------------------------------------------------- fachadas laterais do pátio (prédio dos dois lados)
def lateral(v, facing_right):
    """Parede comprida de um prédio vista de cima e de lado: a laje (borda de cima) e a face com os 3 andares —
    janelas de caixilho branco nos dois de cima, blocos de vidro e portas de vidro no térreo (foto da praça central).
    facing_right: a face olha para a direita (prédio à esquerda do pátio)."""
    cv = Canvas(T, T)
    r = rng(1800 + v + (50 if facing_right else 0))
    WALL, WALL_D, STAIN = hexc("d6cfbf"), hexc("b9b1a0"), hexc("9c9381")
    GLASS, FRAME_W = hexc("1c2633"), hexc("e8e8e4")
    # colunas, da laje (longe do pátio) até o chão (perto do pátio)
    cols = list(range(T)) if facing_right else list(range(T - 1, -1, -1))
    def col(i): return cols[i]
    for y in range(T):
        for i in range(0, 4): cv.put(col(i), y, hexc("bdb9b0"))          # laje
        cv.put(col(4), y, hexc("6e6a62"))                                  # borda da laje
        for i in range(5, 16): cv.put(col(i), y, WALL)
    for y in range(T):
        # 3º andar e 2º andar: faixa de janelas (vidro escuro com montantes brancos a cada 4 px)
        for band in ((6, 7), (9, 10)):
            for i in band:
                c = FRAME_W if y % 4 == 0 else GLASS
                if v == 2 and band == (6, 7) and 4 <= y <= 9: c = hexc("05070a") if y % 4 else FRAME_W  # vidro quebrado
                cv.put(col(i), y, c)
        # térreo: blocos de vidro (faixa alta) ou porta de vidro (variação 1)
        if v == 1:
            for i in (12, 13, 14):
                cv.put(col(i), y, FRAME_W if y % 5 == 0 or i == 12 else hexc("18222d"))
        else:
            cv.put(col(12), y, hexc("cfd6dc") if y % 2 else hexc("8fa1b0"))
        cv.put(col(15), y, WALL_D)                                         # rodapé
    # manchas de chuva e mofo, típicos do concreto da UNIFESP
    for _ in range(5):
        y = r.randrange(T); cv.put(col(r.randrange(5, 12)), y, STAIN)
    if v == 3:  # trepadeira subindo pela parede
        for y in range(T):
            for i in range(11, 16):
                if r.random() < 0.55: cv.put(col(i), y, r.choice([C["leaf"], C["leaf_d"]]))
    return cv


for v in range(4):
    tile(f"lateral_l{v}", v, 20); place(lateral(v, True), f"lateral_l{v}")
    tile(f"lateral_r{v}", 4 + v, 20); place(lateral(v, False), f"lateral_r{v}")

# ---------------------------------------------------------------- mata fechada atrás do campus (copas vistas de cima)
def mata(v):
    cv = Canvas(T, T, hexc("1f3d22"))
    r = rng(1900 + v)
    for _ in range(5):  # copas
        cx, cy, rad = r.randrange(T), r.randrange(T), r.uniform(3, 6)
        for dx in (-T, 0, T):
            for dy in (-T, 0, T):
                cv.ellipse(cx + dx, cy + dy, rad, rad * 0.9, r.choice([hexc("2d5a2c"), hexc("27502a"), hexc("356634")]))
                cv.ellipse(cx + dx - 1, cy + dy - 1, rad * 0.5, rad * 0.45, hexc("4a7d3d"))
    cv.noise(0, 0, T, T, [hexc("16301a"), hexc("3f7236")], 0.12, r)
    return cv


for v in range(3):
    tile(f"mata{v}", v, 21); place(mata(v), f"mata{v}")

# ---------------------------------------------------------------- borda do mapa no meio da avenida
# Mesma cara do asfalto/calçada, mas com colisão (o jogador não sai do mapa e a rua não fica cortada por uma cerca viva).
for j, (name, fn) in enumerate([("topo_via_meiofio0", lambda: floor_bricks(2, curb=True)), ("topo_via_meiofio1", lambda: floor_bricks(3, curb=True)),
                                ("topo_via_asfalto_meiofio", lambda: floor_asphalt(1, under_curb=True)),
                                ("topo_via_asfalto_faixa", lambda: floor_asphalt(0, line=True)), ("topo_via_asfalto", lambda: floor_asphalt(2))]):
    tile(name, 8 + j, 20); place(age(fn(), "asfalto", 900 + j), name)

# linha 0: compatibilidade com o atlas provisório (0/1 piso, 2 topo, 3 frente)
for n, c in [("granito0", 0), ("granito1", 1), ("topo_interno_15", 2), ("frente_interno_3", 3)]:
    col, row = names[n]
    atlas.img.paste(atlas.img.crop((col * T, row * T, col * T + T, row * T + T)), (c * T, 0))

atlas.save("sprites/tiles/fase1_tiles.png")


# ---------------------------------------------------------------- recursos da Godot
# Todo tile de parede precisa ter colisão. A colisão vai pelo começo do nome do tile:
# ao criar um tipo novo de parede, use um destes prefixos (ou acrescente o novo aqui).
# tools/levelgen/check_walls.py confere a cena pronta e acusa parede sem colisão.
WALL_PREFIXES = ("topo_", "frente_", "lateral_")


def write_tileset(path, collision_layer):
    walls = [v for k, v in names.items() if k.startswith(WALL_PREFIXES)] + [[2, 0], [3, 0]]
    lines = ['[gd_resource type="TileSet" format=3]', "",
             '[ext_resource type="Texture2D" path="res://sprites/tiles/fase1_tiles.png" id="1_tex"]', "",
             '[sub_resource type="TileSetAtlasSource" id="atlas"]', 'texture = ExtResource("1_tex")']
    used = sorted({tuple(v) for v in names.values()} | {(0, 0), (1, 0), (2, 0), (3, 0)}, key=lambda p: (p[1], p[0]))
    wallset = {tuple(w) for w in walls}
    for (x, y) in used:
        lines.append(f"{x}:{y}/0 = 0")
        if (x, y) in wallset:
            lines.append(f"{x}:{y}/0/physics_layer_0/polygon_0/points = PackedVector2Array(-8, -8, 8, -8, 8, 8, -8, 8)")
    lines += ["", "[resource]", f"physics_layer_0/collision_layer = {collision_layer}", 'sources/0 = SubResource("atlas")', ""]
    with open(os.path.join(ROOT, path), "w") as f:
        f.write("\n".join(lines))


write_tileset("tilesets/fase1_tileset.tres", 1)
write_tileset("tilesets/fase1_tileset_outer.tres", 9)  # 1 + 8: também segura os fantasmas
with open(os.path.join(ROOT, "tools/levelgen/tiles_fase1.json"), "w") as f:
    json.dump({"tiles": names, "decos": DECOS}, f, indent=1, ensure_ascii=False)
print("tiles:", len(names))
