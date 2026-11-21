# Props da escada (fim da Fase 2): o prédio abandonado desabando atrás do jogador.
# Cada sprite tem o tamanho do jogo dividido por 4 (1 pixel da arte = 4 do jogo).
# Saída: sprites/props/escada/
#   barricada0/1   pilha de carteiras, tábuas e fita zebrada (fecha a faixa: 48 x 14 = 192 x 56 no jogo)
#   lampada        calha fluorescente caindo do teto (28 x 7)
#   lampada_quebrada  a calha estilhaçada no chão (30 x 10)
#   extintor       extintor vermelho deitado, rolando escada abaixo (dois quadros: extintor0/1, 14 x 8)
#   espuma         nuvem de pó quando o extintor leva um tiro (16 x 12)
#   degrau_quebrado  buraco no meio do lance (32 x 32 = 2 x 2 tiles)
#   lance_metade   metade do lance que já caiu (32 x 80 = 2 x 5 tiles)
#   andar0..4      número do andar pintado no chão do patamar (24 x 28)
from pix import Canvas, rng, mix, shade, hexc
from palette import C
from props_fase1 import shadow_ellipse

OUT = "sprites/props/escada/"
INK = C["ink"]
WOOD, WOOD_L, WOOD_D = C["wood"], C["wood_l"], C["wood_d"]
RED, RED_L, RED_D = hexc("c8322b"), hexc("e65a4b"), hexc("7d1c18")
YEL, BLK = hexc("e8c22e"), hexc("1d1e22")
CONC, CONC_L, CONC_D, CONC_K = C["conc"], C["conc_l"], C["conc_d"], C["conc_k"]
VOID = hexc("07080b")
DESK_TOP, DESK_TOP_D = hexc("cfc6b0"), hexc("9c927b")


# ======================================================================== BARRICADA
def barricada(seed):
    """Carteiras de braço tombadas, tábuas pregadas atravessadas e a fita zebrada amarela e preta."""
    w, h = 48, 14
    cv = Canvas(w, h)
    r = rng(5100 + seed)
    shadow_ellipse(cv, w / 2, h - 1.5, w / 2 - 1, 1.6, 90)
    # carteiras tombadas (tampo bege e pés de ferro)
    x = 1
    while x < w - 8:
        dw = r.randrange(9, 13)
        top = r.randrange(2, 5)
        cv.rect(x, top, dw, 4, DESK_TOP); cv.hline(x, top, dw, shade(DESK_TOP, 0.3)); cv.hline(x, top + 3, dw, DESK_TOP_D)
        for lx in (x + 1, x + dw - 2):                    # pés apontando para cima/baixo
            cv.vline(lx, top + 4, h - top - 6, C["steel_d"])
        if r.random() < 0.5:
            cv.line(x + 2, top + 4, x + dw - 3, h - 3, C["steel_k"])
        x += dw - r.randrange(1, 3)
    # tábuas atravessadas
    for i in range(3):
        y0 = r.randrange(1, 6); y1 = r.randrange(6, 11)
        x0 = r.randrange(0, w // 2); x1 = min(w - 1, x0 + r.randrange(18, 30))
        cv.line(x0, y0, x1, y1, WOOD); cv.line(x0, y0 + 1, x1, y1 + 1, WOOD_D)
        cv.put(x0 + 2, y0 + (y1 - y0) * 2 // (x1 - x0 + 1), C["steel_l"])   # prego
    # fita zebrada atravessando tudo
    fy = r.randrange(5, 8)
    for xx in range(w):
        yy = fy + (1 if (xx // 9) % 2 else 0)
        cv.put(xx, yy, YEL if (xx // 3) % 2 == 0 else BLK)
    cv.outline(INK)
    return cv


for s in range(2):
    barricada(s).save(OUT + f"barricada{s}.png")


# ======================================================================== LÂMPADA FLUORESCENTE
def lampada():
    """Calha de lâmpada do teto: chapa branca com duas lâmpadas tubulares, presa por um fio rompido."""
    cv = Canvas(28, 7)
    cv.rect(0, 1, 28, 5, hexc("dcdcd6")); cv.hline(0, 1, 28, hexc("f2f2ee")); cv.hline(0, 5, 28, hexc("9a9a94"))
    for y in (2, 4):
        cv.hline(2, y, 24, hexc("f8fbff"))
        cv.put(2, y, hexc("b8c4cc")); cv.put(25, y, hexc("b8c4cc"))
    cv.line(13, 0, 15, 0, hexc("3a3a3a"))   # fio rompido
    cv.outline(INK)
    return cv


def lampada_quebrada():
    """A calha no chão, torta, com cacos de vidro espalhados e pó branco."""
    cv = Canvas(30, 10)
    r = rng(5200)
    shadow_ellipse(cv, 15, 8, 13, 1.5, 70)
    for x in range(2, 26):
        y = 3 + (1 if x > 13 else 0) + (1 if x > 20 else 0)
        cv.put(x, y, hexc("dcdcd6")); cv.put(x, y + 1, hexc("dcdcd6")); cv.put(x, y + 2, hexc("9a9a94"))
    for x in range(4, 12):
        cv.put(x, 4, hexc("f8fbff"))
    for _ in range(26):   # cacos e pó
        x, y = r.randrange(0, 30), r.randrange(0, 10)
        cv.put(x, y, r.choice([hexc("e8f2fa"), hexc("bcd2e0"), hexc("ffffff", 160)]))
    return cv


lampada().save(OUT + "lampada.png")
lampada_quebrada().save(OUT + "lampada_quebrada.png")


# ======================================================================== EXTINTOR
def extintor(frame):
    """Extintor vermelho deitado (rola para baixo da tela): corpo, faixa branca do rótulo, válvula e mangueira.
    Dois quadros: a faixa do rótulo muda de lugar e dá a sensação de rolar."""
    cv = Canvas(14, 8)
    cv.rect(1, 1, 10, 6, RED); cv.hline(1, 1, 10, RED_L); cv.hline(1, 6, 10, RED_D)
    lx = 4 if frame == 0 else 7
    cv.vline(lx, 1, 6, hexc("eeeeee")); cv.put(lx, 6, hexc("bbbbbb"))
    cv.rect(11, 2, 2, 4, C["steel"]); cv.put(13, 3, BLK); cv.put(13, 4, BLK)   # válvula
    cv.line(11, 6, 9, 7, BLK)                                                  # mangueira
    cv.outline(INK)
    return cv


def espuma():
    """Nuvem de pó químico branco quando o extintor estoura."""
    cv = Canvas(16, 12)
    r = rng(5300)
    for _ in range(9):
        cx, cy = r.randrange(3, 13), r.randrange(3, 9)
        rad = r.randrange(2, 5)
        cv.ellipse(cx, cy, rad, rad * 0.8, hexc("f2f4f6", 210))
    for _ in range(12):
        cv.put(r.randrange(0, 16), r.randrange(0, 12), hexc("ffffff", 180))
    return cv


for f in range(2):
    extintor(f).save(OUT + f"extintor{f}.png")
espuma().save(OUT + "espuma.png")


# ======================================================================== BURACOS
def buraco(w, h, seed, steps=True):
    """Buraco no lance: borda de concreto quebrada (irregular), ferragem aparecendo e o escuro lá embaixo.
    steps: desenha as ranhuras dos degraus em volta, para combinar com o piso da escada."""
    cv = Canvas(w, h)
    r = rng(5400 + seed)
    # contorno irregular: cada linha tem um recuo aleatório dos dois lados
    left = [0] * h; right = [0] * h
    a, b = r.randrange(1, 4), r.randrange(1, 4)
    for y in range(h):
        a = max(1, min(5, a + r.choice([-1, 0, 0, 1]))); b = max(1, min(5, b + r.choice([-1, 0, 0, 1])))
        left[y], right[y] = a, w - 1 - b
    top_cut, bot_cut = r.randrange(1, 4), r.randrange(1, 4)
    for y in range(top_cut, h - bot_cut):
        for x in range(left[y], right[y] + 1):
            cv.put(x, y, VOID)
        cv.put(left[y], y, CONC_K); cv.put(right[y], y, CONC_K)       # beirada quebrada
        if left[y] > 0: cv.put(left[y] - 1, y, CONC_L)
        if right[y] < w - 1: cv.put(right[y] + 1, y, CONC_L)
    for x in range(left[top_cut], right[top_cut] + 1):                # face do degrau de cima (mais clara)
        cv.put(x, top_cut, CONC); cv.put(x, top_cut + 1, CONC_D)
    # ferragem (vergalhões) atravessando o buraco
    for _ in range(max(1, w * h // 400)):
        y = r.randrange(top_cut + 3, h - bot_cut - 2)
        x0, x1 = left[y] - 1, left[y] + r.randrange(4, max(5, (right[y] - left[y]) // 2 + 2))
        cv.line(x0, y, x1, y + r.choice([-1, 0, 1]), hexc("7a4a2e"))
    # pedaços soltos na beirada
    for _ in range(w * h // 60):
        y = r.randrange(top_cut, h - bot_cut)
        x = r.choice([left[y] - 2, right[y] + 2])
        cv.put(x, y, r.choice([CONC_D, CONC_K, CONC_L]))
    return cv


buraco(32, 32, 0).save(OUT + "degrau_quebrado.png")
buraco(32, 80, 1).save(OUT + "lance_metade.png")


# ======================================================================== NÚMEROS DOS ANDARES
DIGITS = {
    "0": ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
    "1": ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
    "2": ["01110", "10001", "00001", "00110", "01000", "10000", "11111"],
    "3": ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
    "4": ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
}


def andar(d):
    """Número do andar pintado em estêncil amarelo no chão do patamar, gasto pelo tempo."""
    cv = Canvas(24, 28)
    r = rng(5500 + int(d))
    paint = hexc("e8c22e", 230)
    for j, row in enumerate(DIGITS[d]):
        for k, bit in enumerate(row):
            if bit == "1":
                for yy in range(3):
                    for xx in range(3):
                        if r.random() > 0.12:   # tinta descascada
                            cv.put(2 + k * 3 + xx, 2 + j * 3 + yy, paint)
    # "º" pequeno ao lado
    for (x, y) in [(19, 2), (20, 2), (18, 3), (21, 3), (19, 4), (20, 4), (18, 6), (19, 6), (20, 6), (21, 6)]:
        cv.put(x, y, paint)
    return cv


for d in "01234":
    andar(d).save(OUT + f"andar{d}.png")

print("props da escada ok")
