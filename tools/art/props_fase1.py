# Props da Fase 1: fachada do ICT, portão, guarita, praça, RU, cantina e elevador.
# Cada sprite tem o tamanho do Size do prop dividido por 4 (1 pixel da arte = 4 do jogo).
# Saída: sprites/props/fase1/*.png
from pix import Canvas, rng, mix, shade, hexc, text, text_center, text_width
from palette import C

OUT = "sprites/props/fase1/"
OUTLINE = hexc("2a2830")


def box(cv, x, y, w, top_h, front_h, top, front, line=None, hi=True):
    """Caixa vista em 3/4: tampo em cima (top_h) e frente embaixo (front_h)."""
    line = line or shade(front, -0.45)
    cv.rect(x, y, w, top_h, top)
    if hi: cv.hline(x + 1, y + 1, w - 2, shade(top, 0.25))
    cv.rect(x, y + top_h, w, front_h, front)
    cv.hline(x, y + top_h, w, shade(front, 0.15))
    cv.frame(x, y, w, top_h + front_h, line)
    cv.hline(x, y + top_h - 1 if top_h > 1 else y, w, shade(top, -0.25))


def shadow_ellipse(cv, cx, cy, rx, ry, a=70):
    cv.ellipse(cx, cy, rx, ry, hexc("000000", a))


# ======================================================================== ENTRADA
def predio():
    """Fachada do prédio do ICT vista da entrada: concreto bege, faixa de janelas e o letreiro sem o N e o S."""
    W, H = 320, 60
    cv = Canvas(W, H)
    r = rng(1)
    # laje (topo) com platibanda
    cv.rect(0, 0, W, 14, C["conc_d"])
    cv.noise(1, 1, W - 2, 12, [C["conc_k"], C["conc_stain"]], 0.15, r)
    cv.rect(0, 0, W, 2, C["conc_l"]); cv.hline(0, 13, W, C["conc_k"])
    for x0 in (40, 120, 230):  # condensadoras do ar-condicionado no telhado
        cv.rect(x0, 4, 12, 7, hexc("d9dbd7")); cv.frame(x0, 4, 12, 7, hexc("8d908c"))
        cv.ellipse(x0 + 6, 7.5, 3, 2.5, hexc("7a7d79")); cv.put(x0 + 6, 7, hexc("b9bcb8"))
    cv.rect(280, 3, 20, 9, hexc("8fa5b8")); cv.frame(280, 3, 20, 9, hexc("5f7486"))  # caixa d'água
    # frente
    cv.rect(0, 14, W, H - 14, C["conc"])
    cv.noise(0, 14, W, H - 14, [C["conc_stain"], C["conc_l"]], 0.10, r)
    for x in range(0, W, 40):  # juntas das placas de concreto
        cv.vline(x, 15, 18, C["conc_stain"])
    cv.hline(0, 14, W, C["conc_l"])
    # faixa de janelas (marca dos prédios do campus)
    cv.rect(0, 34, W, 9, C["frame"])
    for x0 in range(2, W - 6, 8):
        cv.rect(x0, 35, 6, 7, C["glass"]); cv.vline(x0, 35, 7, C["glass_l"])
        if r.random() < 0.3: cv.put(x0 + 2, 36, C["glass_hi"])
    cv.hline(0, 43, W, C["conc_k"])
    # térreo: vidro com portas no meio e pilares
    cv.rect(0, 44, W, 16, C["conc_d"])
    for x0 in range(120, 200, 10):
        cv.rect(x0, 46, 9, 13, C["glass"]); cv.frame(x0, 46, 9, 13, C["frame"])
        cv.line(x0 + 2, 48, x0 + 4, 50, C["glass_hi"])
    cv.rect(150, 46, 20, 13, hexc("22303e")); cv.vline(160, 46, 13, C["frame"]); cv.frame(150, 46, 20, 13, C["frame"])
    for x0 in (20, 60, 100, 210, 250, 290):
        cv.rect(x0, 44, 4, 16, C["conc_l"]); cv.vline(x0 + 3, 44, 16, C["conc_k"])
    cv.hline(0, H - 1, W, C["conc_k"])
    # letreiro: placa branca, ícone azul e "UNIFESP" (o N e o S caíram no chão)
    px, py = 12, 17
    cv.rect(px, py, 96, 15, hexc("f3f2ee")); cv.frame(px, py, 96, 15, hexc("b9b6ad"))
    cv.rect(px + 3, py + 3, 9, 9, C["uni_blue"]); cv.rect(px + 5, py + 5, 5, 5, C["uni_blue_l"])
    cv.rect(px + 6, py + 6, 3, 3, hexc("f3f2ee"))
    big = {"U": ["1001", "1001", "1001", "1001", "1001", "1111"], "N": ["1001", "1101", "1101", "1011", "1011", "1001"],
           "I": ["111", "010", "010", "010", "010", "111"], "F": ["1111", "1000", "1110", "1000", "1000", "1000"],
           "E": ["1111", "1000", "1110", "1000", "1000", "1111"], "S": ["0111", "1000", "0110", "0001", "0001", "1110"],
           "P": ["1110", "1001", "1001", "1110", "1000", "1000"]}
    x = px + 15
    for ch in "UNIFESP":
        g = big[ch]
        missing = ch in "NS"
        for j, row in enumerate(g):
            for k, b in enumerate(row):
                if b == "1":
                    if missing:  # marca mais clara onde a letra ficava, e os parafusos
                        if j in (0, 5) and k in (0, len(row) - 1): cv.put(x + k, py + 3 + j, hexc("9c9890"))
                        elif r.random() < 0.25: cv.put(x + k, py + 3 + j, hexc("dddad3"))
                    else:
                        cv.put(x + k, py + 3 + j, C["uni_text"])
        x += len(g[0]) + 2
    text(cv, "UNIVERSIDADE FEDERAL", px + 15, py + 10 - 0, hexc("6d717a"))
    # placa do campus à direita
    text(cv, "CAMPUS SAO JOSE DOS CAMPOS", 176, 20, hexc("7d7a70"))
    text(cv, "INSTITUTO DE CIENCIA E TECNOLOGIA", 176, 27, hexc("7d7a70"))
    # manchas de chuva escorrendo
    for _ in range(18):
        x0 = r.randrange(W); cv.vline(x0, 15, r.randrange(3, 14), C["conc_stain"])
    return cv


def letra_caida(ch):
    """Letra de metal do letreiro caída de costas no chão (achatada pela vista de cima)."""
    cv = Canvas(22, 20)
    rows = {"N": ["1100011", "1110011", "1111011", "1101111", "1100111", "1100011", "1100011"],
            "S": ["0111110", "1100011", "1100000", "0111110", "0000011", "1100011", "0111110"]}[ch]
    shadow_ellipse(cv, 11, 13, 10, 5, 55)
    for j, row in enumerate(rows):
        for k, b in enumerate(row):
            if b == "1":
                x, y = 4 + k * 2, 2 + j * 2 - (k // 3)
                cv.rect(x, y + 1, 2, 2, C["uni_text"])
                if j + 1 >= len(rows) or rows[j + 1][k] != "1": cv.hline(x, y + 3, 2, hexc("1b1d22"))
                if j == 0 or rows[j - 1][k] != "1": cv.hline(x, y + 1, 2, hexc("7a818c"))
    cv.put(2, 12, hexc("9a9da3")); cv.put(19, 4, hexc("9a9da3"))  # parafusos soltos
    return cv


def gradil(w=240, h=18):
    """Gradil verde do campus (as grades e portões da foto da fachada)."""
    cv = Canvas(w, h)
    cv.rect(0, h - 4, w, 4, hexc("b9b6ad")); cv.hline(0, h - 4, w, hexc("d6d3cb")); cv.hline(0, h - 1, w, hexc("8a877f"))
    cv.hline(0, 1, w, C["fence_l"]); cv.hline(0, 2, w, C["fence_d"])
    cv.hline(0, h - 6, w, C["fence_l"]); cv.hline(0, h - 5, w, C["fence_d"])
    for x in range(0, w, 2):
        cv.vline(x, 3, h - 9, C["fence"])
    for x in range(0, w, 30):
        cv.rect(x, 0, 2, h - 4, C["fence_d"]); cv.put(x, 0, C["fence_l"])
    return cv


def poste_portao():
    cv = Canvas(7, 32)
    cv.rect(1, 2, 5, 28, C["fence"]); cv.vline(1, 2, 28, C["fence_l"]); cv.vline(5, 2, 28, C["fence_d"])
    cv.rect(0, 0, 7, 3, C["fence_d"]); cv.hline(1, 0, 5, C["fence_l"])
    cv.rect(0, 29, 7, 3, hexc("8a877f"))
    return cv


def placa_portao():
    cv = Canvas(65, 9)
    cv.rect(0, 0, 65, 9, hexc("f3f2ee")); cv.frame(0, 0, 65, 9, C["fence_d"])
    cv.rect(2, 2, 5, 5, C["uni_blue"]); cv.put(4, 4, hexc("f3f2ee"))
    text(cv, "UNIFESP", 10, 2, C["uni_text"]); text(cv, "ICT", 50, 2, C["fence"])
    cv.put(44, 4, C["uni_text"])
    return cv


def ponto_onibus():
    """Abrigo verde do ponto de ônibus da avenida (foto da fachada)."""
    cv = Canvas(48, 34)
    shadow_ellipse(cv, 24, 31, 24, 3, 60)
    cv.rect(2, 0, 44, 7, C["fence"]); cv.hline(2, 0, 44, C["fence_l"]); cv.hline(2, 6, 44, C["fence_d"])
    cv.rect(4, 7, 40, 18, mix(C["glass_l"], hexc("ffffff"), 0.35))
    cv.rect(6, 9, 14, 12, hexc("d8e6c6")); cv.rect(7, 10, 12, 6, hexc("6fae4d")); text(cv, "ICT", 8, 16, C["fence_d"])
    for x in (3, 44): cv.rect(x, 7, 2, 25, C["fence_d"])
    cv.rect(10, 25, 28, 3, C["wood"]); cv.hline(10, 25, 28, C["wood_l"]); cv.rect(11, 28, 2, 4, C["black"]); cv.rect(35, 28, 2, 4, C["black"])
    return cv


def guarita():
    """Portaria: bloco de concreto com janela do porteiro, placa e catraca ao lado."""
    W, H = 160, 80
    cv = Canvas(W, H)
    r = rng(5)
    cv.rect(0, 0, W, 52, C["conc_d"]); cv.noise(1, 1, W - 2, 50, [C["conc_k"], C["conc_stain"]], 0.12, r)
    cv.rect(0, 0, W, 3, C["conc_l"]); cv.vline(0, 0, 52, C["conc_l"]); cv.vline(W - 1, 0, 52, C["conc_k"])
    cv.rect(110, 12, 18, 12, hexc("d9dbd7")); cv.frame(110, 12, 18, 12, hexc("8d908c"))
    cv.ellipse(119, 18, 5, 4, hexc("7a7d79"))
    cv.line(30, 8, 30, 20, hexc("6a6d72")); cv.line(27, 10, 33, 10, hexc("6a6d72"))  # antena
    cv.rect(50, 30, 40, 14, hexc("c7c0ae")); cv.frame(50, 30, 40, 14, C["conc_k"])  # alçapão
    cv.hline(0, 51, W, C["conc_k"])
    cv.rect(0, 52, W, 28, C["conc"]); cv.noise(0, 52, W, 28, [C["conc_stain"]], 0.1, r)
    # janelão do porteiro com o vidro escuro e o porteiro
    cv.rect(14, 56, 70, 18, C["frame"]); cv.rect(16, 58, 66, 14, C["glass"])
    cv.rect(40, 62, 8, 10, hexc("3e4d5f")); cv.ellipse(44, 61, 3, 3, hexc("4b5b6e"))  # silhueta
    for x0 in (16, 38, 60): cv.line(x0 + 2, 58, x0 + 8, 64, C["glass_l"])
    cv.rect(14, 73, 70, 2, hexc("e7e3d9"))  # balcão da janela
    # placa PORTARIA
    cv.rect(98, 56, 52, 9, C["fence"]); cv.frame(98, 56, 52, 9, C["fence_d"])
    text_center(cv, "PORTARIA", 124, 58, hexc("f2fff5"))
    cv.rect(110, 67, 14, 13, hexc("8e959d")); cv.rect(111, 68, 12, 12, hexc("6b737c")); cv.put(121, 74, hexc("d9dde1"))
    cv.hline(0, H - 1, W, C["conc_k"])
    return cv


# ======================================================================== PRAÇA (corredor)
def palmeira(cv, cx, base_y, height, r):
    """Palmeira-real pequena como as da praça: tronco anelado e folhas grandes caindo."""
    for y in range(base_y - height, base_y):
        cv.put(cx - 1, y, C["trunk"]); cv.put(cx, y, C["trunk"]); cv.put(cx + 1, y, C["trunk_d"])
        if (base_y - y) % 3 == 0: cv.hline(cx - 1, y, 3, C["trunk_d"])
    top = base_y - height
    fronds = [(-1, -0.9, 12), (1, -0.9, 12), (-1, 0.1, 14), (1, 0.1, 14), (-0.4, -1.3, 9), (0.4, -1.3, 9), (-0.9, 0.7, 10), (0.9, 0.7, 10), (0, 0.9, 7)]
    for dx, dy, L in fronds:
        pts = []
        for t in range(L + 1):
            x = cx + dx * t * 1.1
            y = top + dy * t * 0.8 + (t * t) * 0.06  # a ponta cai
            pts.append((int(round(x)), int(round(y))))
        for i, (x, y) in enumerate(pts):
            cv.put(x, y, C["leaf"]); cv.put(x, y + 1, C["leaf_d"])
            if i % 2 == 0 and i > 1:  # folíolos
                cv.put(x, y + 2, C["leaf_d"]); cv.put(x, y - 1, C["leaf_l"])
    cv.ellipse(cx + 0.5, top + 0.5, 3, 2.5, C["leaf"]); cv.ellipse(cx, top, 2, 1.5, C["leaf_l"]); cv.put(cx, top, hexc("9fd06a"))


def agave(cv, cx, cy):
    for dx, dy in [(-4, -1), (-3, -3), (0, -5), (3, -3), (4, -1), (-1, -3), (1, -3)]:
        cv.line(cx, cy, cx + dx, cy + dy, C["agave"]); cv.put(cx + dx, cy + dy, hexc("a8d0b0"))
    cv.put(cx, cy, C["agave_d"]); cv.put(cx - 1, cy, C["agave_d"]); cv.put(cx + 1, cy, C["agave_d"])


def jardim(w=32, h=96):
    """Canteiro de concreto branco da praça, com grama, agaves e palmeiras (foto da praça central)."""
    extra = 26
    cv = Canvas(w + 12, h + extra)
    ox = 6
    r = rng(7)
    y0 = extra
    cv.rect(ox, y0, w, h, hexc("e7e3d8")); cv.frame(ox, y0, w, h, hexc("b8b2a4"))
    cv.rect(ox, y0 + h - 4, w, 4, hexc("cfc9bb")); cv.hline(ox, y0 + h - 4, w, hexc("f2efe7"))
    cv.rect(ox + 3, y0 + 3, w - 6, h - 9, C["grass"])
    cv.noise(ox + 3, y0 + 3, w - 6, h - 9, [C["grass_l"], C["grass_d"]], 0.35, r)
    for (ax, ay) in [(8, 22), (23, 36), (9, 60), (23, 78), (12, 46)]:
        agave(cv, ox + ax, y0 + ay)
    palmeira(cv, ox + 16, y0 + 20, 30, r)
    palmeira(cv, ox + 15, y0 + 70, 26, r)
    return cv


def poste_luz():
    """Poste branco com globo (os da praça)."""
    cv = Canvas(8, 44)
    shadow_ellipse(cv, 4, 42, 4, 1.5, 60)
    cv.rect(3, 8, 2, 34, hexc("3a3d44")); cv.vline(3, 8, 34, hexc("5a5e66"))
    cv.ellipse(4, 5, 4, 4.5, hexc("f4f3ee")); cv.ellipse(3, 4, 1.5, 1.5, hexc("ffffff"))
    cv.rect(2, 41, 4, 2, hexc("2a2c31"))
    return cv


def caixa_dagua():
    """A caixa d'água verde, cilindro alto que aparece de quase todo o campus."""
    cv = Canvas(20, 72)
    shadow_ellipse(cv, 10, 69, 10, 3, 60)
    cv.rect(3, 6, 14, 63, hexc("7f9a5a")); cv.vline(3, 6, 63, hexc("a2bd7a")); cv.vline(4, 6, 63, hexc("93ae6c"))
    cv.vline(16, 6, 63, hexc("5f7843")); cv.vline(15, 6, 63, hexc("6f8a50"))
    cv.ellipse(10, 6, 7, 3, hexc("94ad70")); cv.ellipse(10, 6, 5, 2, hexc("a9c283"))
    for y in range(14, 68, 10): cv.hline(3, y, 14, hexc("6f8a50"))
    return cv


def lixeiras(n=4):
    """Lixeiras da coleta seletiva (vermelha, azul, amarela, verde)."""
    cols = [C["bin_red"], C["bin_blue"], C["bin_yellow"], C["bin_green"]][:n]
    cv = Canvas(5 * n + 1, 12)
    shadow_ellipse(cv, (5 * n + 1) / 2, 11, (5 * n) / 2, 1.5, 60)
    for i, c in enumerate(cols):
        x = 1 + i * 5
        cv.rect(x, 3, 4, 8, c); cv.vline(x, 3, 8, shade(c, 0.25)); cv.vline(x + 3, 3, 8, shade(c, -0.3))
        cv.rect(x - 0, 1, 4, 2, shade(c, -0.2)); cv.hline(x, 1, 4, shade(c, 0.2))
        cv.put(x + 1, 6, hexc("ffffff", 160))
    return cv


# ======================================================================== RU
def balcao_devolucao():
    W, H = 80, 22
    cv = Canvas(W, H)
    box(cv, 0, 0, W, 8, 14, C["inox_l"], C["inox"])
    cv.rect(4, 1, 30, 6, hexc("2b2f35"))  # janela da devolução (cozinha escura)
    for i in range(4):  # bandejas empilhadas
        cv.rect(40 + i, 5 - i, 14, 2, hexc("c9a46a") if i % 2 else hexc("b48d55"))
    for k in range(3):
        cv.rect(58 + k * 6, 2, 5, 4, hexc("f5f5f0")); cv.frame(58 + k * 6, 2, 5, 4, hexc("c7c7c0"))
    cv.rect(6, 11, 40, 6, hexc("2f6b45")); text(cv, "DEVOLUCAO", 8, 12, hexc("ecfff2"))
    for x in range(52, 78, 4): cv.vline(x, 10, 10, C["inox_d"])
    return cv


def microondas():
    W, H = 28, 24
    cv = Canvas(W, H)
    box(cv, 0, 6, W, 6, 12, C["table"], C["table_d"])
    for x0 in (1, 14):
        cv.rect(x0, 0, 13, 9, hexc("f1f1ee")); cv.frame(x0, 0, 13, 9, hexc("8d9096"))
        cv.rect(x0 + 1, 1, 8, 7, hexc("26292e")); cv.put(x0 + 2, 2, hexc("5f6670"))
        cv.rect(x0 + 10, 2, 2, 1, hexc("3fdc6a")); cv.rect(x0 + 10, 4, 2, 3, hexc("b7bbc1"))
    text(cv, "RU", 10, 15, C["table_k"])
    return cv


def balcao_comida():
    """Bufê do RU: cubas de aço com arroz, feijão, salada, carne e farofa, e o vidro protetor."""
    W, H = 64, 24
    cv = Canvas(W, H)
    box(cv, 0, 4, W, 9, 11, C["inox_l"], C["inox"])
    foods = [hexc("f4f1e6"), hexc("5a3424"), hexc("4fa33a"), hexc("8e4a2a"), hexc("e0b453"), hexc("e66a3a")]
    for i, f in enumerate(foods):
        x = 2 + i * 10
        cv.rect(x, 6, 9, 5, C["inox_d"]); cv.rect(x + 1, 7, 7, 3, f)
        cv.put(x + 2, 7, shade(f, 0.3)); cv.put(x + 5, 8, shade(f, -0.2))
    cv.rect(1, 0, W - 2, 3, hexc("cfe6f2", 140)); cv.hline(1, 0, W - 2, hexc("ffffff", 200))
    for x in (1, W - 2): cv.vline(x, 0, 6, C["inox_d"])
    for x in range(4, W, 8): cv.vline(x, 15, 7, C["inox_d"])
    return cv


def mesa_ru():
    """Mesa comprida do RU com os banquinhos redondos presos (foto do RU)."""
    W, H = 48, 16
    cv = Canvas(W, H)
    for x in range(4, W - 2, 8):  # banquinhos de trás
        cv.ellipse(x + 2, 2, 2.5, 1.5, hexc("b9bdbd")); cv.put(x + 2, 1, hexc("d9dcdc"))
    box(cv, 0, 3, W, 6, 2, C["table"], C["table_d"])
    cv.rect(3, 11, 2, 2, C["black"]); cv.rect(W - 5, 11, 2, 2, C["black"]); cv.hline(3, 12, W - 6, C["black"])
    for x in range(4, W - 2, 8):  # banquinhos da frente
        cv.vline(x + 2, 12, 2, C["black"])
        cv.ellipse(x + 2, 14, 2.5, 1.5, hexc("c9cdcd")); cv.put(x + 1, 13, hexc("e5e8e8")); cv.put(x + 3, 15, hexc("8f9393"))
    return cv


def cadeira_ru(label="R.U."):
    cv = Canvas(8, 12)
    shadow_ellipse(cv, 4, 11, 4, 1.2, 60)
    cv.rect(1, 0, 6, 6, C["black"]); cv.frame(1, 0, 6, 6, hexc("17181b")); cv.hline(2, 1, 4, C["black_l"])
    cv.rect(1, 6, 6, 2, C["black_l"]); cv.vline(1, 8, 3, C["black"]); cv.vline(6, 8, 3, C["black"])
    cv.put(3, 3, hexc("d8d8d8")); cv.put(4, 3, hexc("d8d8d8"))
    return cv


# ======================================================================== CANTINA
def balcao_cantina(w=288, h=24):
    """Balcão da cantina: vitrines de salgados, caixa, geladeira de bebidas e cafeteira."""
    cv = Canvas(w, h)
    r = rng(11)
    box(cv, 0, 4, w, 7, 13, hexc("d7d0c0"), hexc("f1efe9"))
    cv.hline(0, 17, w, hexc("b5b0a4")); cv.rect(0, 18, w, 6, hexc("cfcac0")); cv.hline(0, h - 1, w, hexc("8f8a80"))
    # vitrines de vidro com salgados
    salg = [hexc("d98a3a"), hexc("e7c27a"), hexc("c56b2c"), hexc("f1d38c")]
    for x0 in range(10, w - 60, 46):
        cv.rect(x0, 0, 36, 10, hexc("cfe6f2", 150)); cv.frame(x0, 0, 36, 10, C["inox_d"])
        cv.hline(x0 + 1, 1, 34, hexc("ffffff", 200))
        for k in range(8):
            c = r.choice(salg); xx = x0 + 3 + k * 4
            cv.rect(xx, 6, 3, 2, c); cv.put(xx + 1, 5, c); cv.put(xx, 7, shade(c, -0.3))
        cv.rect(x0, 10, 36, 1, C["inox_d"])
    # caixa registradora e cafeteira
    cx = w - 52
    cv.rect(cx, 0, 12, 8, hexc("34373d")); cv.rect(cx + 1, 1, 10, 3, hexc("7fe0a0")); cv.rect(cx + 2, 5, 8, 2, hexc("50545c"))
    cv.rect(cx + 18, 0, 10, 9, hexc("2b2c31")); cv.rect(cx + 20, 2, 6, 3, hexc("c33b2c")); cv.put(cx + 22, 7, hexc("e9e9e9"))
    # geladeira de bebidas na ponta
    gx = w - 20
    cv.rect(gx, 0, 18, 18, hexc("d9dde1")); cv.frame(gx, 0, 18, 18, hexc("7f868e"))
    cv.rect(gx + 2, 2, 14, 13, hexc("9fd3ef", 200))
    for row in range(3):
        for k in range(4):
            cv.rect(gx + 3 + k * 3, 3 + row * 4, 2, 3, [hexc("d6342a"), hexc("2b9a4a"), hexc("f2c230"), hexc("2f6fd1")][(k + row) % 4])
    text(cv, "CANTINA", 60, 13, hexc("2f6b45"))
    return cv


def balcao_lateral():
    cv = Canvas(16, 32)
    box(cv, 0, 0, 16, 22, 10, hexc("d7d0c0"), hexc("f1efe9"))
    return cv


def letreiro_cantina():
    cv = Canvas(90, 12)
    cv.rect(0, 0, 90, 12, hexc("22324a")); cv.frame(0, 0, 90, 12, hexc("0f1724"))
    for i, ch in enumerate("CANTINA"):
        text(cv, ch, 22 + i * 7, 4, hexc("f7d24a"))
    for x in range(3, 88, 6): cv.put(x, 2, hexc("ffef9a"))
    return cv


def mesa_cantina():
    """Mesa redonda branca com as cadeiras pretas escritas CANTINA."""
    cv = Canvas(24, 18)
    shadow_ellipse(cv, 12, 16, 11, 2, 55)

    def chair(x, y, back_up):
        cv.rect(x, y, 6, 4, C["black"]); cv.hline(x + 1, y + 1, 4, C["black_l"])
        if back_up: cv.rect(x, y - 4, 6, 4, hexc("1f2024")); cv.hline(x + 1, y - 3, 4, C["black_l"])
        cv.vline(x, y + 4, 2, C["black"]); cv.vline(x + 5, y + 4, 2, C["black"])
    chair(0, 7, True); chair(18, 7, True)
    cv.ellipse(12, 7, 9, 4.6, hexc("3a3c42")); cv.ellipse(12, 7, 8, 4, C["table_d"]); cv.ellipse(12, 6, 8, 3.6, C["table"])
    cv.hline(7, 4, 6, hexc("ffffff")); cv.ellipse(15, 7, 2, 1, hexc("b8b2a0"))  # mancha de café no tampo
    cv.vline(12, 10, 5, C["black"]); cv.hline(9, 15, 7, C["black"])
    chair(9, 12, False)
    return cv


def sinuca():
    """Mesa de sinuca de madeira com feltro verde (tem duas na cantina)."""
    W, H = 48, 20
    cv = Canvas(W, H)
    shadow_ellipse(cv, 24, 18, 24, 2, 60)
    cv.rect(0, 0, W, 14, C["wood"]); cv.frame(0, 0, W, 14, C["wood_d"]); cv.hline(1, 1, W - 2, C["wood_l"])
    cv.rect(3, 3, W - 6, 8, C["felt"]); cv.hline(3, 3, W - 6, C["felt_d"]); cv.noise(3, 4, W - 6, 7, [C["felt_l"]], 0.06, rng(3))
    for (x, y) in [(2, 2), (W // 2 - 1, 2), (W - 4, 2), (2, 10), (W // 2 - 1, 10), (W - 4, 10)]:
        cv.rect(x, y, 2, 2, hexc("101012"))
    for (x, y, c) in [(30, 6, "f2f2f2"), (14, 5, "e23a2a"), (15, 7, "f2c230"), (13, 7, "2f5fd1"), (16, 5, "1a1a1a")]:
        cv.put(x, y, hexc(c))
    cv.line(31, 7, 40, 9, hexc("e3c48c"))  # taco
    cv.rect(0, 14, W, 3, C["wood_d"])
    for x in (2, W - 6): cv.rect(x, 14, 4, 5, C["wood_d"]); cv.vline(x, 14, 5, C["wood"])
    return cv


def bebedouro():
    cv = Canvas(8, 16)
    shadow_ellipse(cv, 4, 15, 4, 1.2, 60)
    cv.rect(1, 3, 6, 12, hexc("e9ebec")); cv.frame(1, 3, 6, 12, hexc("9da3a9"))
    cv.rect(2, 0, 4, 4, hexc("8fc6ea")); cv.put(2, 0, hexc("c9e6f7"))
    cv.rect(2, 6, 4, 2, hexc("4d555e")); cv.put(3, 9, hexc("2f8fd1"))
    return cv


def sofa():
    cv = Canvas(32, 14)
    shadow_ellipse(cv, 16, 13, 16, 1.5, 60)
    cv.rect(0, 0, 32, 6, hexc("4a4f59")); cv.rect(0, 6, 32, 6, hexc("5c6270"))
    cv.hline(1, 1, 30, hexc("646a77")); cv.vline(16, 6, 6, hexc("4a4f59"))
    cv.rect(0, 2, 3, 10, hexc("40444d")); cv.rect(29, 2, 3, 10, hexc("40444d"))
    return cv


# ======================================================================== ELEVADOR
def porta_elevador():
    """Portas de inox do elevador na parede, com o painel do andar."""
    cv = Canvas(32, 16)
    cv.rect(2, 2, 28, 14, hexc("aab1b8")); cv.frame(2, 2, 28, 14, hexc("6b727a"))
    cv.rect(4, 4, 11, 12, C["inox"]); cv.rect(17, 4, 11, 12, C["inox"])
    cv.vline(16, 4, 12, hexc("5e656d")); cv.vline(5, 4, 12, C["inox_l"]); cv.vline(18, 4, 12, C["inox_l"])
    cv.rect(12, 0, 8, 3, hexc("1d2026")); text(cv, "1", 14, -1, hexc("ff6a3a"))
    cv.put(13, 1, hexc("ff6a3a"))
    return cv


# ======================================================================== v2: fachada real, abandono, RU das fotos
SERIF = {  # letras do letreiro (fonte serifada, como na fachada de verdade), 7 px de altura
    "U": ["11   11", " 1   1 ", " 1   1 ", " 1   1 ", " 1   1 ", " 1   1 ", "  111  "],
    "N": ["11   11", " 11   1", " 1 1  1", " 1  1 1", " 1   11", " 1    1", "111   1"],
    "I": ["111", " 1 ", " 1 ", " 1 ", " 1 ", " 1 ", "111"],
    "F": ["111111", " 1   1", " 1    ", " 1111 ", " 1    ", " 1    ", "111   "],
    "E": ["111111", " 1   1", " 1    ", " 1111 ", " 1    ", " 1   1", "111111"],
    "S": [" 1111 ", "1    1", "1     ", " 1111 ", "     1", "1    1", " 1111 "],
    "P": ["11111 ", " 1   1", " 1   1", " 1111 ", " 1    ", " 1    ", "111   "],
}
LETTER = hexc("2b2a2c")        # letras de metal escuro
LETTER_HI = hexc("56545a")
LOGO = ["    11    ", "  111111  ", " 11111111 ", "1111111111", " 1 1 1 1 1", " 1 1 1 1 1", "1111111111"]
LOGO_G, LOGO_GL = hexc("1f6a3c"), hexc("3a8c58")
WALL_W, WALL_W2, WALL_J = hexc("d9d8d2"), hexc("cfcec7"), hexc("bdbbb3")  # concreto claro da fachada
BRISE, BRISE_D = hexc("a9adb2"), hexc("7f848b")
LETTERS_X0 = 30  # onde começa "UNIFESP" na fachada (pixel da arte)


LS = 2  # cada "pixel" do desenho da letra vira 2x2 (o letreiro é grande na fachada)


def glyph(cv, ch, x, y, c=LETTER, hi=LETTER_HI):
    for j, row in enumerate(SERIF[ch]):
        for k, b in enumerate(row):
            if b == "1":
                cv.rect(x + k * LS, y + j * LS, LS, LS, c)
                if j == 0 or SERIF[ch][j - 1][k] != "1": cv.hline(x + k * LS, y + j * LS, LS, hi)
                if j == len(SERIF[ch]) - 1 or SERIF[ch][j + 1][k] != "1": cv.hline(x + k * LS, y + j * LS + LS - 1, LS, hexc("161518"))


def glyph_mark(cv, ch, x, y, r):
    """Onde a letra caiu: o concreto ficou mais limpo no formato dela, com os furos dos parafusos e ferrugem escorrendo."""
    rows = SERIF[ch]
    for j, row in enumerate(rows):
        for k, b in enumerate(row):
            if b == "1": cv.rect(x + k * LS, y + j * LS, LS, LS, hexc("ebeae5"))
    for (k, j) in [(1, 0), (len(rows[0]) - 2, 0), (1, 6), (len(rows[0]) - 2, 6)]:
        px_, py_ = x + k * LS, y + j * LS
        cv.put(px_, py_, hexc("2a2420"))
        for d in range(1, 6 + (k % 3)):
            cv.put(px_, py_ + d, hexc("8a5a36", 150 - d * 20))


def predio():
    """Prédio do ICT visto da entrada: concreto claro em placas, brises de alumínio, janelas de bloco de vidro,
    o logo verde e as letras escuras (sem o N e o S, que caíram), marquise da entrada. Abandonado: manchas, rachaduras,
    trepadeira, vidros quebrados."""
    W, H = 320, 60
    cv = Canvas(W, H)
    r = rng(21)
    # laje
    cv.rect(0, 0, W, 12, hexc("b9b7b0")); cv.noise(1, 1, W - 2, 10, [hexc("aaa8a1"), hexc("c6c4bd")], 0.3, r)
    cv.rect(0, 0, W, 2, hexc("e2e1db")); cv.hline(0, 11, W, hexc("8f8d86"))
    for x0 in (60, 132, 250):
        cv.rect(x0, 3, 12, 7, hexc("9a9c98")); cv.frame(x0, 3, 12, 7, hexc("6a6c68")); cv.ellipse(x0 + 6, 6.5, 3, 2.5, hexc("55575a"))
    cv.rect(200, 2, 16, 8, hexc("6f8ea6")); cv.frame(200, 2, 16, 8, hexc("4a6276"))  # caixas d'água azuis
    cv.rect(218, 2, 16, 8, hexc("6f8ea6")); cv.frame(218, 2, 16, 8, hexc("4a6276"))
    # parede em placas
    cv.rect(0, 12, W, H - 12, WALL_W)
    for y in range(12, H, 6): cv.hline(0, y, W, WALL_J)
    for y in range(12, H, 6):
        off = 0 if (y // 6) % 2 else 10
        for x in range(off, W, 20): cv.vline(x, y, 6, WALL_J)
    cv.noise(0, 12, W, H - 12, [WALL_W2], 0.12, r)
    # bloco com brises (lâminas verticais de alumínio) no meio da fachada
    bx0, bx1 = 140, 236
    cv.rect(bx0, 14, bx1 - bx0, 30, BRISE)
    for x in range(bx0, bx1, 2): cv.vline(x, 14, 30, BRISE_D)
    for x in (bx0 + 30, bx0 + 62): cv.rect(x, 22, 12, 10, hexc("23262b"))  # vãos escuros atrás das lâminas
    cv.hline(bx0, 14, bx1 - bx0, hexc("cdd0d4"))
    # janelas de bloco de vidro em faixas
    for (x0, y0, w) in [(8, 50, 84), (246, 20, 66), (246, 30, 66), (246, 40, 66)]:
        cv.rect(x0, y0, w, 4, hexc("e9ecee"))
        for x in range(x0 + 1, x0 + w - 1, 2):
            cv.put(x, y0 + 1, hexc("8aa0b2")); cv.put(x, y0 + 2, hexc("5d7487"))
    for x in r.sample(range(10, 90, 2), 7):  # blocos quebrados
        cv.put(x, 51, hexc("0b0d10")); cv.put(x, 52, hexc("0b0d10"))
    # letreiro: logo + UNIFESP (o N e o S caíram) + duas linhas de texto
    ly = 15
    for j, row in enumerate(LOGO):  # logo verde (o prédio com colunas), também em 2x2
        for k, b in enumerate(row):
            if b == "1": cv.rect(8 + k * 2, ly + j * 2, 2, 2, LOGO_GL if j < 3 else LOGO_G)
    x = LETTERS_X0
    for ch in "UNIFESP":
        if ch in "NS": glyph_mark(cv, ch, x, ly, r)
        else: glyph(cv, ch, x, ly)
        x += (len(SERIF[ch][0]) + 1) * LS
    text(cv, "UNIVERSIDADE FEDERAL DE SAO PAULO", 8, ly + 17, hexc("38383c"))
    text(cv, "CAMPUS SAO JOSE DOS CAMPOS", 8, ly + 24, hexc("38383c"))
    cv.put(102, ly + 16, hexc("38383c")); cv.put(103, ly + 16, hexc("38383c"))  # til do SÃO
    # marquise da entrada: volume baixo branco com vidro escuro
    mx = 96
    cv.rect(mx, 44, 120, 16, hexc("e4e3de")); cv.hline(mx, 44, 120, hexc("f2f1ec")); cv.hline(mx, 47, 120, hexc("b3b1aa"))
    cv.rect(mx + 30, 48, 60, 12, hexc("12171d")); cv.frame(mx + 30, 48, 60, 12, hexc("8e959d"))
    for x in range(mx + 40, mx + 90, 12): cv.vline(x, 48, 12, hexc("8e959d"))
    cv.line(mx + 33, 50, mx + 38, 55, hexc("6f8aa3")); cv.line(mx + 70, 49, mx + 66, 57, hexc("9fb6cc"))  # vidro trincado
    cv.rect(mx + 52, 52, 8, 8, hexc("050608"))  # porta arrombada
    # abandono: escorridos, rachaduras, trepadeira, pichação
    for _ in range(40):
        x0 = r.randrange(W); cv.vline(x0, 12, r.randrange(4, 22), hexc("8f8a7c", 80))
    for (x, y) in [(118, 14), (236, 30), (40, 44)]:
        for _ in range(14):
            cv.put(x, y, hexc("6a675f")); x += r.choice((-1, 0, 1)); y += 1
    for x in range(0, W):
        hgt = int(max(0, 10 * abs(((x * 13) % 29) / 29 - 0.5) * 2 - 2 + r.random() * 6)) if (x < 90 or x > 230) else 0
        for y in range(H - hgt, H):
            if r.random() < 0.7: cv.put(x, y, r.choice([C["leaf_d"], C["leaf"], hexc("2f4f2a")]))
    text(cv, "FORMATURA CANCELADA", 244, 50, hexc("9e2a2a"))
    return cv


def letra_caida(ch):
    """A letra que caiu do letreiro: o mesmo desenho e a mesma cor das que ficaram, deitada torta no chão,
    com cacos de concreto em volta."""
    from PIL import Image
    g = Canvas(16, 16)
    glyph(g, ch, 1, 1)
    g.img = g.img.rotate(-14 if ch == "N" else 7, resample=Image.NEAREST, expand=True)
    g.px = g.img.load()
    # deitada no chão: achata na vertical (vista de cima em 3/4)
    g.img = g.img.resize((g.img.width, int(g.img.height * 0.85)), Image.NEAREST); g.px = g.img.load()
    g.w, g.h = g.img.size
    cv = Canvas(30, 24)
    shadow_ellipse(cv, 15, 15, 13, 6, 60)
    for y in range(g.h):  # espessura (a letra é de metal com alguns centímetros)
        for x in range(g.w):
            if g.get(x, y)[3]: cv.put(4 + x, 2 + y + 2, hexc("121114")); cv.put(4 + x, 2 + y + 1, hexc("121114"))
    cv.blit(g, 4, 2)
    r = rng(ord(ch))
    for _ in range(10):  # cacos de concreto que vieram junto
        x, y = r.randrange(1, 29), r.randrange(10, 23)
        cv.rect(x, y, 2, 1, WALL_W); cv.put(x, y + 1, WALL_J)
    cv.put(3, 20, hexc("6b6f75")); cv.put(26, 8, hexc("6b6f75"))  # parafusos
    return cv


def poste_globo(cv, x, base_y, lit=True):
    cv.rect(x, base_y - 34, 2, 34, hexc("2b2d32")); cv.vline(x, base_y - 34, 34, hexc("4a4d54"))
    cv.ellipse(x + 1, base_y - 37, 3.5, 4, hexc("e8e6dd") if lit else hexc("6e6c66"))
    if lit: cv.ellipse(x, base_y - 38, 1.5, 1.5, hexc("ffffff"))
    else: cv.put(x + 2, base_y - 36, hexc("2b2d32")); cv.put(x, base_y - 39, hexc("2b2d32"))  # globo quebrado
    cv.rect(x - 1, base_y - 1, 4, 2, hexc("2a2c31"))


def jardim(w=32, h=96, lamps=((0, 6, True), (1, 86, False))):
    """Canteiro da praça: borda de concreto (suja), grama alta, agaves, palmeiras e os postes de globo dentro dele,
    como nas fotos. lamps: (lado 0=esquerda/1=direita, altura do pé, aceso)."""
    extra = 30
    cv = Canvas(w + 12, h + extra)
    r = rng(7)
    ox, y0 = 6, extra
    cv.rect(ox, y0, w, h, hexc("cfccc2")); cv.frame(ox, y0, w, h, hexc("9d988a"))
    cv.rect(ox, y0 + h - 4, w, 4, hexc("b9b5a8")); cv.hline(ox, y0 + h - 4, w, hexc("dedad0"))
    cv.noise(ox, y0, w, h, [hexc("8f8a7c"), hexc("7a8a62")], 0.06, r)  # musgo e sujeira na borda
    cv.rect(ox + 3, y0 + 3, w - 6, h - 9, C["grass_d"])
    cv.noise(ox + 3, y0 + 3, w - 6, h - 9, [C["grass"], C["grass_k"], hexc("6d7a3a")], 0.45, r)
    for _ in range(30):  # mato alto passando da borda
        x, y = r.randrange(ox + 1, ox + w - 1), r.randrange(y0 + 1, y0 + h - 4)
        for k in range(r.randrange(2, 5)): cv.put(x, y - k, r.choice([C["grass"], C["grass_l"], hexc("7f8f45")]))
    for (ax, ay) in [(8, 22), (23, 36), (9, 60), (23, 78), (12, 46)]:
        agave(cv, ox + ax, y0 + ay)
    palmeira(cv, ox + 16, y0 + 22, 30, r)
    palmeira(cv, ox + 15, y0 + 70, 26, r)
    for side, ly, lit in lamps:
        poste_globo(cv, ox + (4 if side == 0 else w - 6), y0 + ly, lit)
    return cv


def poste_rua(lit=True):
    """Poste de rua com braço e luminária de vapor de sódio (avenida em frente ao campus)."""
    cv = Canvas(20, 60)
    shadow_ellipse(cv, 4, 58, 4, 1.5, 70)
    cv.rect(3, 6, 3, 52, hexc("8f9296")); cv.vline(3, 6, 52, hexc("b3b6ba")); cv.vline(5, 6, 52, hexc("6a6d72"))
    cv.line(5, 7, 15, 3, hexc("8f9296")); cv.line(5, 8, 15, 4, hexc("6a6d72"))
    cv.rect(12, 2, 7, 3, hexc("4a4d52")); cv.hline(13, 5, 5, hexc("ffcf7a") if lit else hexc("3a3b3e"))
    for y in (20, 34, 47): cv.hline(3, y, 3, hexc("7a6a52"))  # ferrugem
    return cv


def carro(variant):
    """Carro abandonado no estacionamento (visto de cima, de frente para cima): pneu murcho, para-brisa quebrado,
    poeira e folhas em cima."""
    body, dark = [(hexc("8a3a2e"), hexc("5e261e")), (hexc("6d7a80"), hexc("47525a")), (hexc("c9c4b3"), hexc("8f8a7b"))][variant]
    cv = Canvas(26, 34)
    shadow_ellipse(cv, 13, 31, 12, 3, 80)
    cv.rect(3, 3, 20, 28, body); cv.frame(3, 3, 20, 28, dark)
    cv.rect(4, 2, 18, 2, dark)
    cv.rect(5, 7, 16, 6, hexc("1a1f26")); cv.line(6, 8, 12, 12, hexc("8fa9bf")); cv.line(14, 7, 10, 12, hexc("8fa9bf"))  # para-brisa trincado
    cv.rect(5, 14, 16, 8, shade(body, 0.12)); cv.hline(5, 14, 16, shade(body, 0.3))  # teto
    cv.rect(5, 23, 16, 4, hexc("1a1f26"))
    for (x, y) in [(1, 6), (22, 6), (1, 23), (22, 23)]:
        cv.rect(x, y, 3, 6, hexc("1b1b1e"))
    cv.rect(22, 23, 3, 6, hexc("101012")); cv.put(23, 29, hexc("101012"))  # pneu murcho
    r = rng(30 + variant)
    cv.noise(4, 4, 18, 26, [hexc("9a8f72", 120), hexc("6a5a34", 140)], 0.12, r)  # poeira e folhas
    for x in range(4, 22, 3):  # ferrugem nas bordas
        if r.random() < 0.6: cv.put(x, 30, hexc("7a4422"))
    return cv


# ---------------------------------------------------------------- RU (fotos do tour)
STOOL, STOOL_D = hexc("d8d6cd"), hexc("9b998f")
FRAME = hexc("1c1d21")


def mesa_ru_longa(w=80):
    """Mesa comprida branca do RU com os banquinhos redondos presos em braços de ferro preto, dos dois lados."""
    cv = Canvas(w, 20)
    r = rng(40 + w)
    for x in range(5, w - 4, 9):  # banquinhos de trás
        cv.line(x + 2, 4, x + 2, 6, FRAME)
        cv.ellipse(x + 2, 3, 3, 1.8, STOOL); cv.hline(x, 4, 5, STOOL_D)
    cv.rect(0, 5, w, 7, C["table"]); cv.hline(0, 5, w, hexc("f8f8f4")); cv.hline(0, 11, w, hexc("b9c4b8"))
    cv.frame(0, 5, w, 7, hexc("6d746c"))
    cv.rect(0, 12, w, 1, hexc("9ea79c"))
    cv.hline(2, 13, w - 4, FRAME); cv.vline(2, 12, 6, FRAME); cv.vline(w - 3, 12, 6, FRAME)
    for x in range(5, w - 4, 9):  # banquinhos da frente
        cv.line(x + 2, 13, x + 1, 15, FRAME)
        cv.ellipse(x + 1, 16, 3, 1.8, STOOL); cv.hline(x - 1, 17, 5, STOOL_D); cv.put(x, 15, hexc("f0eee6"))
    # abandono: um banquinho arrancado, poeira, bandeja esquecida
    cv.rect(w // 2 - 2, 15, 6, 4, (0, 0, 0, 0)); cv.put(w // 2, 17, FRAME)
    cv.noise(0, 5, w, 7, [hexc("c8c2b0")], 0.08, r)
    cv.rect(w // 3, 7, 6, 3, hexc("b48d55")); cv.hline(w // 3, 7, 6, hexc("c9a46a"))
    return cv


def mesa_ru():
    return mesa_ru_longa(48)


def mesa_ru_cadeiras():
    """Mesa branca do fundo do RU com as cadeiras pretas escritas R.U. (uma caída)."""
    W = 48
    cv = Canvas(W, 22)
    for x in (4, 18, 32):  # cadeiras de trás (encosto aparece por cima da mesa)
        cv.rect(x, 0, 8, 6, C["black"]); cv.hline(x + 1, 1, 6, C["black_l"]); cv.put(x + 3, 3, hexc("d8d8d8")); cv.put(x + 4, 3, hexc("d8d8d8"))
    cv.rect(0, 6, W, 7, C["table"]); cv.hline(0, 6, W, hexc("f8f8f4")); cv.hline(0, 12, W, C["table_d"])
    cv.frame(0, 6, W, 7, hexc("6d746c"))
    for x in (1, W - 2): cv.vline(x, 13, 6, hexc("8a8e92"))
    for x in (6, 30):  # cadeiras da frente
        cv.rect(x, 14, 8, 4, C["black"]); cv.hline(x + 1, 14, 6, C["black_l"]); cv.vline(x, 18, 3, C["black"]); cv.vline(x + 7, 18, 3, C["black"])
    # cadeira caída de lado
    cv.rect(18, 17, 9, 3, C["black"]); cv.rect(25, 13, 3, 6, C["black"]); cv.hline(18, 20, 9, hexc("101012"))
    return cv


def balcao_comida():
    """Bufê do RU: tampo de inox, vidro protetor, frente de madeira escura; as cubas vazias e sujas."""
    W, H = 64, 24
    cv = Canvas(W, H)
    cv.rect(0, 4, W, 8, C["inox_l"]); cv.frame(0, 4, W, 8, C["inox_d"])
    for i in range(6):
        x = 2 + i * 10
        cv.rect(x, 6, 9, 5, C["inox_d"]); cv.rect(x + 1, 7, 7, 3, hexc("3d3a34") if i % 2 else hexc("5a4a32"))
    cv.rect(1, 0, W - 2, 3, hexc("cfe6f2", 110)); cv.hline(1, 0, W - 2, hexc("ffffff", 160))
    cv.line(20, 0, 26, 2, hexc("ffffff", 200))  # vidro trincado
    for x in (1, W - 2): cv.vline(x, 0, 6, C["inox_d"])
    cv.rect(0, 12, W, 12, hexc("5b3b22")); cv.frame(0, 12, W, 12, hexc("3b2414"))
    for x in range(0, W, 16): cv.vline(x, 12, 12, hexc("3b2414")); cv.hline(x + 2, 14, 12, hexc("7a5434"))
    cv.hline(0, H - 1, W, hexc("2a1a0f"))
    return cv


def lixeiras_pedal():
    """As duas lixeiras brancas de pedal do RU."""
    cv = Canvas(16, 12)
    shadow_ellipse(cv, 8, 11, 8, 1.5, 60)
    for x, lid in ((1, hexc("4f86cf")), (9, hexc("9aa0a6"))):
        cv.rect(x, 3, 6, 8, hexc("eceeee")); cv.vline(x + 5, 3, 8, hexc("b9bdc0")); cv.rect(x, 1, 6, 2, lid)
        cv.put(x + 2, 10, hexc("5a5f66"))
    return cv


def catraca_ru():
    """Catraca na entrada do RU, ao lado da divisória de vidro (quebrada, braço caído)."""
    cv = Canvas(16, 20)
    shadow_ellipse(cv, 8, 18, 7, 1.5, 60)
    cv.rect(3, 4, 7, 14, hexc("b9c0c7")); cv.hline(3, 4, 7, hexc("e1e6ea")); cv.vline(9, 4, 14, hexc("7e868f"))
    cv.rect(4, 6, 5, 3, hexc("2a2d33")); cv.put(5, 7, hexc("ff3b30"))
    cv.line(10, 11, 15, 13, hexc("d9dee3")); cv.line(10, 12, 15, 14, hexc("8a929b"))
    cv.line(10, 15, 12, 18, hexc("d9dee3"))  # braço caído
    cv.rect(0, 2, 2, 16, hexc("aeb5bc"))
    return cv


def cadeira_caida():
    cv = Canvas(12, 8)
    shadow_ellipse(cv, 6, 6, 6, 1.5, 50)
    cv.rect(1, 3, 9, 3, C["black"]); cv.rect(8, 0, 3, 5, C["black"]); cv.hline(1, 3, 9, C["black_l"])
    cv.put(2, 6, C["black"]); cv.put(6, 6, C["black"])
    return cv


def entulho():
    """Entulho: pedaços do forro, fios e cacos (canto da cantina e do RU)."""
    cv = Canvas(20, 12)
    r = rng(55)
    shadow_ellipse(cv, 10, 9, 9, 2.5, 50)
    for _ in range(8):
        x, y = r.randrange(1, 16), r.randrange(2, 9)
        cv.rect(x, y, r.randrange(2, 5), 2, r.choice([hexc("e9e8e2"), hexc("cfcdc4"), hexc("b8b6ad")]))
    cv.line(3, 3, 12, 8, hexc("2a2b30")); cv.line(12, 8, 17, 6, hexc("c0392b"))
    return cv


SPRITES = {
    "predio": predio, "letra_n": lambda: letra_caida("N"), "letra_s": lambda: letra_caida("S"),
    "gradil": gradil, "poste_portao": poste_portao, "placa_portao": placa_portao, "ponto_onibus": ponto_onibus,
    "guarita": guarita, "jardim": jardim, "caixa_dagua": caixa_dagua,
    "lixeiras": lixeiras, "balcao_devolucao": balcao_devolucao, "microondas": microondas,
    "balcao_comida": balcao_comida,
    "balcao_cantina": balcao_cantina, "balcao_lateral": balcao_lateral, "letreiro_cantina": letreiro_cantina,
    "mesa_cantina": mesa_cantina, "sinuca": sinuca, "bebedouro": bebedouro, "sofa": sofa,
    "porta_elevador": porta_elevador,
    "jardim_b": lambda: jardim(lamps=((1, 8, False), (0, 84, True))),
    "poste_rua": poste_rua, "poste_rua_apagado": lambda: poste_rua(False),
    "carro0": lambda: carro(0), "carro1": lambda: carro(1), "carro2": lambda: carro(2),
    "mesa_ru_longa": mesa_ru_longa, "mesa_ru_cadeiras": mesa_ru_cadeiras, "lixeiras_pedal": lixeiras_pedal,
    "catraca_ru": catraca_ru, "cadeira_caida": cadeira_caida, "entulho": entulho,
}

if __name__ == "__main__":
    for name, fn in SPRITES.items():
        fn().save(OUT + name + ".png")
    print(len(SPRITES), "props")
