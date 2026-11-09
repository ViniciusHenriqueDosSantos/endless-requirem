# Props, chefe e itens da Fase 2 (o andar da biblioteca do ICT), à noite e abandonado.
# Referência: fotos do tour 360° (biblioteca: estantes de aço bege, mesas brancas com cadeiras pretas, xadrez gigante,
# mural colorido, salas de estudo de vidro escuro; corredor: mãos pintadas, quadro verde, armários azuis).
# Cada sprite tem o Size do prop dividido por 4 (1 pixel da arte = 4 do jogo).
# Saída: sprites/props/fase2/, sprites/characters/bibliotecaria.png, sprites/fx/livros.png,
#        sprites/items/ (livro, chaves) e sprites/items/inspecao/
from pix import Canvas, rng, mix, shade, hexc, text, text_center, sheet
from palette import C
from props_fase1 import box, shadow_ellipse

OUT = "sprites/props/fase2/"
INK = C["ink"]
BEGE_MET, BEGE_MET_L, BEGE_MET_D = hexc("d9cfb8"), hexc("ebe3cf"), hexc("a99d84")   # estante de aço bege
BOOKS = [hexc("b33a2e"), hexc("2f5fa8"), hexc("3f8a4a"), hexc("d9a62e"), hexc("6a3f8f"), hexc("e3e0d6"),
         hexc("1f7a7a"), hexc("8a5a3c"), hexc("c0567f"), hexc("2a2b30")]
WHITE_T, WHITE_T_D = hexc("eef0ec"), hexc("b9bdb7")


# ======================================================================== BIBLIOTECA
def estante(seed=0, w=128, h=32):
    """Estante dupla de aço bege, comprida, com livros coloridos. Décadas depois: buracos, livros tombados,
    teia de aranha e uma prateleira entortada."""
    cv = Canvas(w, h)
    r = rng(3000 + seed)
    shadow_ellipse(cv, w / 2, h - 1.5, w / 2 - 2, 1.5, 80)
    top_h = 5
    cv.rect(0, 0, w, top_h, BEGE_MET); cv.hline(0, 0, w, BEGE_MET_L); cv.hline(0, top_h - 1, w, BEGE_MET_D)
    for x in range(2, w - 2, 3):  # livros vistos de cima (lombadas das duas faces)
        if r.random() < 0.8: cv.vline(x, 1, 3, r.choice(BOOKS))
    front_y = top_h
    cv.rect(0, front_y, w, h - front_y - 1, BEGE_MET_D)
    shelves = [front_y + 1, front_y + 9, front_y + 17]
    for i, sy in enumerate(shelves):
        x = 2
        while x < w - 2:
            if r.random() < 0.12:   # buraco: livros que caíram
                x += r.randrange(3, 8); continue
            bw = r.randrange(1, 3); bh = r.randrange(5, 8)
            c = r.choice(BOOKS)
            if r.random() < 0.06:  # livro tombado
                cv.rect(x, sy + 7 - 2, 5, 2, c); x += 5; continue
            cv.rect(x, sy + 7 - bh, bw, bh, c)
            cv.put(x, sy + 7 - bh, shade(c, 0.3))
            x += bw
        cv.rect(0, sy + 7, w, 1, BEGE_MET); cv.hline(0, sy + 7, w, BEGE_MET_L)
    for x in (0, w // 4, w // 2, 3 * w // 4, w - 1):   # montantes
        cv.vline(x, front_y, h - front_y - 1, BEGE_MET)
        cv.vline(min(x + 1, w - 1), front_y, h - front_y - 1, BEGE_MET_D)
    cv.rect(0, h - 3, w, 2, BEGE_MET_D); cv.hline(0, h - 2, w, shade(BEGE_MET_D, -0.3))
    # teias de aranha nos cantos
    for (x0, flip) in ((1, 1), (w - 2, -1)):
        for k in range(5): cv.put(x0 + k * flip, front_y + 1 + k, hexc("e9ecef", 120)); cv.put(x0 + k * flip, front_y + 1, hexc("e9ecef", 90))
    if seed % 2:  # prateleira entortada
        cv.line(w // 2 + 4, shelves[1] + 7, w // 2 + 20, shelves[1] + 9, BEGE_MET_D)
    cv.frame(0, 0, w, h - 1, shade(BEGE_MET_D, -0.45))
    return cv


def cadeira_preta(cv, x, y, facing_up=True, fallen=False):
    """Cadeira preta de escritório (as da biblioteca), vista de cima em 3/4."""
    B, BL = C["black"], C["black_l"]
    if fallen:
        cv.rect(x, y + 3, 7, 3, B); cv.hline(x, y + 3, 7, BL); cv.rect(x + 6, y + 1, 2, 5, B); return
    if facing_up:  # encosto para o lado de baixo (a pessoa olha para a mesa em cima)
        cv.rect(x, y, 6, 3, B); cv.hline(x, y, 6, BL); cv.rect(x, y + 3, 6, 3, BL); cv.hline(x, y + 5, 6, B)
    else:
        cv.rect(x, y, 6, 3, BL); cv.hline(x, y, 6, B); cv.rect(x, y + 3, 6, 3, B); cv.hline(x, y + 3, 6, BL)
    cv.put(x + 1, y + 6, B); cv.put(x + 4, y + 6, B)


def mesa_estudo(w=32, h=15, seed=0):
    """Mesa branca de estudo com as cadeiras pretas (uma caída), papéis e um copo esquecidos."""
    cv = Canvas(w, h)
    r = rng(3100 + seed)
    shadow_ellipse(cv, w / 2, h - 2, w / 2 - 1, 2, 70)
    for i, x in enumerate(range(3, w - 6, 9)):
        cadeira_preta(cv, x, 0, facing_up=False)
    box(cv, 0, 3, w, 6, 2, WHITE_T, WHITE_T_D)
    cv.vline(1, 11, 3, C["black"]); cv.vline(w - 2, 11, 3, C["black"])
    cv.rect(r.randrange(3, w - 8), 4, 4, 3, hexc("e2ddcf")); cv.put(r.randrange(4, w - 6), 5, hexc("2f5fa8"))
    cv.rect(w - 7, 4, 2, 2, hexc("b33a2e"))   # copo
    for i, x in enumerate(range(4, w - 6, 10)):
        cadeira_preta(cv, x, 9, facing_up=True, fallen=(i == 1 and seed % 2 == 0))
    return cv


def mesa_redonda():
    """Mesa redonda branca de pé central, com cadeiras pretas (canto de convivência perto do mural)."""
    cv = Canvas(24, 18)
    shadow_ellipse(cv, 12, 15, 10, 2.5, 70)
    cadeira_preta(cv, 3, 1, facing_up=False); cadeira_preta(cv, 15, 1, facing_up=False)
    cv.ellipse(12, 8, 9, 4, WHITE_T); cv.ellipse(12, 9, 9, 4, WHITE_T_D); cv.ellipse(12, 8, 9, 3.5, WHITE_T)
    cv.vline(12, 12, 3, C["black"]); cv.hline(9, 15, 7, C["black"])
    cadeira_preta(cv, 2, 10, fallen=True); cadeira_preta(cv, 16, 11)
    return cv


def balcao_bib():
    """Balcão de atendimento da biblioteca: computadores antigos apagados e o carrinho de devolução."""
    W, H = 48, 20
    cv = Canvas(W, H)
    shadow_ellipse(cv, W / 2, H - 2, W / 2 - 1, 2, 80)
    box(cv, 0, 6, W, 5, 8, hexc("dcd8cc"), hexc("9aa0a6"))
    for x in (4, 18, 32):  # monitores
        cv.rect(x, 1, 9, 6, hexc("2a2b30")); cv.rect(x + 1, 2, 7, 4, hexc("10161f")); cv.put(x + 2, 2, hexc("3b5577"))
        cv.vline(x + 4, 7, 1, hexc("2a2b30"))
    cv.rect(14, 7, 3, 2, hexc("e2ddcf")); cv.rect(28, 8, 5, 1, hexc("2a2b30"))
    cv.rect(2, 12, 6, 2, hexc("e2ddcf"))  # aviso de "devolução" colado na frente
    return cv


def caixa_devolucao():
    """Caixa de devolução de livros: madeira escura, a boca de metal e a plaquinha."""
    cv = Canvas(16, 20)
    shadow_ellipse(cv, 8, 18, 7, 1.5, 80)
    box(cv, 1, 3, 14, 3, 13, C["wood_d"], C["wood"])
    cv.rect(3, 7, 10, 2, hexc("1a1714")); cv.hline(3, 9, 10, hexc("9aa1a8"))
    cv.rect(4, 11, 8, 3, hexc("e9e4d6")); cv.hline(5, 12, 6, hexc("3a3f48"))
    cv.put(13, 15, hexc("4a3020")); cv.put(2, 17, hexc("4a3020"))
    return cv


def peca(kind, fallen=False, dark=False):
    """Peça do xadrez gigante (de plástico), em pé ou tombada."""
    body = hexc("26272c") if dark else hexc("e9e6dc")
    hi, lo = shade(body, 0.25 if dark else 0.1), shade(body, -0.3)
    cv = Canvas(12, 20)
    shadow_ellipse(cv, 6, 18, 5, 1.5, 80)
    if fallen:
        cv = Canvas(20, 10)
        shadow_ellipse(cv, 10, 8, 9, 1.5, 80)
        cv.rect(2, 3, 14, 4, body); cv.hline(2, 3, 14, hi); cv.hline(2, 6, 14, lo)
        cv.ellipse(16, 5, 3, 3, body); cv.rect(0, 2, 3, 6, body)
        cv.outline(INK); return cv
    cv.rect(2, 15, 8, 3, body); cv.hline(2, 15, 8, hi); cv.hline(2, 17, 8, lo)
    cv.rect(4, 8, 4, 7, body); cv.vline(4, 8, 7, hi); cv.vline(7, 8, 7, lo)
    if kind == "peao":
        cv.ellipse(6, 6, 3, 3, body); cv.put(5, 5, hi)
    elif kind == "torre":
        cv.rect(3, 4, 6, 4, body); cv.put(3, 3, body); cv.put(5, 3, body); cv.put(8, 3, body); cv.hline(3, 4, 6, hi)
    elif kind == "cavalo":
        cv.rect(4, 3, 4, 5, body); cv.rect(2, 4, 3, 2, body); cv.put(6, 2, body); cv.put(5, 4, lo)
    elif kind == "rei":
        cv.ellipse(6, 5, 3, 3, body); cv.vline(6, 0, 3, body); cv.hline(5, 1, 3, body)
    cv.outline(INK)
    return cv


def placa_molhado():
    """Placa amarela de piso molhado (tem duas no tour; aqui o chão molha de verdade: o teto pinga)."""
    cv = Canvas(8, 11)
    shadow_ellipse(cv, 4, 10, 3.5, 1, 70)
    for y in range(0, 10):
        w = 2 + y // 2
        cv.hline(4 - w // 2, y, w, hexc("f0c92e"))
    cv.vline(4, 3, 3, hexc("2a2b30")); cv.put(4, 7, hexc("2a2b30"))
    cv.outline(INK)
    return cv


def armarios_azuis():
    """Guarda-volumes azuis em frente à biblioteca (três colunas de portinhas, uma arrombada)."""
    W, H = 24, 30
    cv = Canvas(W, H)
    shadow_ellipse(cv, W / 2, H - 1.5, W / 2 - 1, 1.5, 80)
    box(cv, 0, 0, W, 3, H - 4, hexc("3a64b4"), hexc("2b4f9c"))
    for x in (0, 8, 16, W - 1): cv.vline(x, 3, H - 4, hexc("1b3466"))
    for y in range(3, H - 1, 6): cv.hline(0, y, W, hexc("1b3466"))
    for x in (5, 13, 21):
        for y in range(5, H - 2, 6): cv.put(x, y, hexc("c9ced3"))
    cv.rect(9, 10, 7, 5, hexc("0b1120")); cv.rect(15, 10, 3, 5, hexc("2b4f9c")); cv.vline(17, 10, 5, hexc("4f7ad0"))  # porta aberta
    cv.rect(1, 22, 7, 5, hexc("0b1120"))
    return cv


def elevador(broken=False):
    """Porta do elevador na parede da esquerda (vista de cima, de lado): batente de inox e as duas folhas.
    Quebrado: as folhas forçadas, abertas pela metade, e o poço escuro com a luz de emergência."""
    W, H = 12, 48
    cv = Canvas(W, H)
    cv.rect(0, 0, W, H, C["cap"]); cv.vline(0, 0, H, C["cap_d"]); cv.vline(W - 1, 0, H, C["cap_d"])
    cv.rect(2, 4, 8, H - 8, hexc("2a2d32"))
    if broken:
        cv.rect(3, 5, 5, H - 10, hexc("07080a"))
        cv.vline(8, 5, H - 10, hexc("3a0e0c")); cv.vline(7, 12, 20, hexc("5a1512"))
        for y0 in (5, H - 14):  # as folhas, empurradas
            cv.rect(3, y0, 6, 9, C["inox"]); cv.hline(3, y0, 6, C["inox_l"]); cv.hline(3, y0 + 8, 6, C["inox_d"])
        cv.line(4, 18, 6, 24, hexc("8a929b"))  # cabo pendurado
    else:
        cv.rect(3, 5, 6, H - 10, C["inox"]); cv.vline(3, 5, H - 10, C["inox_l"]); cv.vline(8, 5, H - 10, C["inox_d"])
        cv.hline(3, H // 2, 6, C["inox_d"])
        for _ in range(8):  # ferrugem e sujeira
            pass
        cv.rect(1, H // 2 - 4, 1, 3, hexc("e65a3a"))  # botão aceso: o único que ainda funciona
    cv.hline(2, 4, 8, hexc("7e868f")); cv.hline(2, H - 5, 8, hexc("7e868f"))
    return cv


def porta_escada_f2():
    """Porta corta-fogo da escada (lado do corredor): folha de aço cinza, barra antipânico e a plaquinha verde."""
    W, H = 12, 48
    cv = Canvas(W, H)
    cv.rect(0, 0, W, H, C["cap"]); cv.vline(0, 0, H, C["cap_d"]); cv.vline(W - 1, 0, H, C["cap_d"])
    cv.rect(2, 4, 8, H - 8, hexc("3b3f45"))
    cv.rect(3, 5, 5, H - 10, hexc("5e6b66")); cv.vline(3, 5, H - 10, hexc("839690")); cv.vline(7, 5, H - 10, hexc("3a4441"))
    cv.vline(2, H // 2 - 6, 12, hexc("c9cfd4"))
    cv.rect(4, 8, 3, 2, hexc("1f9a52")); cv.put(5, 8, hexc("e9fff0"))
    cv.rect(4, H - 12, 3, 4, hexc("d8ad2e"))   # cadeado
    cv.hline(2, 4, 8, hexc("7e868f")); cv.hline(2, H - 5, 8, hexc("7e868f"))
    return cv


# ======================================================================== HALL DAS CARONAS / ACHADOS
def cadeira_espera(fallen=False):
    """Cadeira preta de espera (das longarinas do hall), vista de cima."""
    cv = Canvas(11, 13)
    shadow_ellipse(cv, 5.5, 11.5, 5, 1.5, 70)
    B, BL = C["black"], C["black_l"]
    if fallen:
        cv.rect(1, 6, 9, 4, B); cv.hline(1, 6, 9, BL); cv.rect(8, 3, 2, 7, B)
    else:
        cv.rect(1, 1, 9, 4, B); cv.hline(1, 1, 9, BL)      # encosto
        cv.rect(1, 5, 9, 4, BL); cv.hline(1, 8, 9, B)       # assento
        for x in (2, 4, 6, 8): cv.put(x, 6, hexc("55575f"))  # furinhos
        cv.vline(1, 9, 3, hexc("8a929b")); cv.vline(9, 9, 3, hexc("8a929b"))
    cv.outline(hexc("111216"))
    return cv


def lousa():
    """Lousa verde dos Achados e Perdidos, com a lista a giz (meio apagada)."""
    W, H = 44, 13
    cv = Canvas(W, H)
    cv.rect(0, 0, W, H - 1, C["wood_d"]); cv.rect(1, 1, W - 2, H - 3, hexc("24493a"))
    r = rng(3300)
    for i, y in enumerate((2, 5, 8)):
        x = 3
        while x < W - 6:
            n = r.randrange(2, 6)
            if r.random() < 0.8: cv.hline(x, y, n, hexc("c9d6cc", 200 - i * 40))
            x += n + 2
    text(cv, "ACHADOS", 3, 2, hexc("e8efe9"), spacing=0) if False else None
    cv.hline(0, H - 2, W, C["wood"]); cv.rect(4, H - 2, 3, 1, hexc("f2f2ee"))   # giz
    return cv


def armario_madeira():
    """Armário alto de madeira com uma porta empenada."""
    W, H = 15, 32
    cv = Canvas(W, H)
    shadow_ellipse(cv, W / 2, H - 1.5, W / 2 - 1, 1.5, 80)
    box(cv, 0, 0, W, 3, H - 4, C["wood_l"], C["wood"])
    cv.vline(7, 3, H - 5, C["wood_d"]); cv.put(5, 15, hexc("d9c38a")); cv.put(9, 15, hexc("d9c38a"))
    cv.rect(8, 4, 6, H - 7, hexc("2a1a10")); cv.line(13, 4, 14, H - 4, C["wood_l"])  # porta aberta, escuro lá dentro
    cv.rect(9, 10, 3, 2, hexc("6a3f8f")); cv.rect(9, 18, 4, 2, hexc("d9a62e"))
    return cv


def mesa_achados():
    cv = Canvas(28, 13)
    shadow_ellipse(cv, 14, 11, 13, 1.5, 70)
    box(cv, 0, 0, 28, 6, 4, C["wood_l"], C["wood"])
    cv.vline(1, 10, 2, C["wood_d"]); cv.vline(26, 10, 2, C["wood_d"])
    cv.rect(4, 1, 5, 3, hexc("e2ddcf")); cv.rect(15, 2, 6, 2, hexc("2a2b30")); cv.put(22, 1, hexc("d9a62e"))
    return cv


def caixa_papelao(state):
    """Caixa de papelão dos Achados: fechada, amassada (levou tiro) e aberta/rasgada."""
    W, H = 14, 14
    cv = Canvas(W, H)
    shadow_ellipse(cv, 7, 12.5, 6, 1.3, 80)
    B, BD, BL = hexc("a8794a"), hexc("7a5532"), hexc("c49563")
    if state == 2:
        box(cv, 1, 5, 12, 2, 6, hexc("3a2a1a"), B, BD, hi=False)
        cv.line(1, 5, 0, 1, BL); cv.line(12, 5, 13, 2, BL); cv.hline(3, 5, 8, BD)
        for (x, y, c) in [(4, 8, "e2ddcf"), (8, 9, "2f5fa8"), (6, 10, "b33a2e")]: cv.put(x, y, hexc(c))
    else:
        box(cv, 1, 2, 12, 4, 7, BL, B, BD)
        cv.vline(7, 2, 4, hexc("d9c9a0")); cv.hline(1, 4, 12, hexc("d9c9a0", 120))  # fita
        cv.rect(3, 8, 4, 2, hexc("e9e4d6"))  # etiqueta
        if state == 1:
            cv.line(9, 6, 12, 10, BD); cv.rect(10, 7, 2, 2, hexc("2a1a10"))
    cv.outline(hexc("2a1a10"))
    return cv


# ======================================================================== LUMINÁRIA (ShieldLamp)
def luminaria(on=True):
    """Luminária de mesa das salas de estudo: haste articulada e a cúpula (a lâmpada fica no alto, ~70 px)."""
    cv = Canvas(14, 22)
    shadow_ellipse(cv, 7, 20.5, 6, 1.3, 70)
    M, ML = hexc("3a3d44"), hexc("5d626b")
    cv.rect(2, 18, 10, 2, M); cv.hline(2, 18, 10, ML)
    cv.line(7, 18, 4, 10, M); cv.line(4, 10, 8, 4, M)
    cv.rect(6, 2, 6, 3, hexc("2f6b45")); cv.hline(6, 2, 6, hexc("4f9a6a"))   # cúpula verde de biblioteca
    cv.hline(7, 5, 4, hexc("fff2b0") if on else hexc("4a4a4a"))
    cv.outline(INK)
    return cv


# ======================================================================== BIBLIOTECÁRIA (chefe)
# Quadros (24x42): 0 e 1 = andando (flutua), 2 = arremessando livro, 3 = caçando (olhos vermelhos, cabelo solto)
def bibliotecaria(frame):
    cv = Canvas(24, 42)
    bob = [0, -1, 0, 0][frame]
    y = 2 + bob
    SK, SK_D = hexc("cdd6dc"), hexc("9aa8b4")        # pele pálida de fantasma
    CARD, CARD_D, CARD_L = hexc("6a5a8a"), hexc("4a3e66"), hexc("8a7aac")  # cardigã roxo
    SKIRT, SKIRT_D = hexc("3a3446"), hexc("2a2534")
    HAIR = hexc("8e8a96")
    cv.ellipse(12, 40, 9, 1.6, hexc("000000", 70))
    # saia longa que vira névoa embaixo (não tem pés)
    for yy in range(y + 24, y + 38):
        w = 6 + (yy - y - 24) // 2
        for xx in range(12 - w, 12 + w):
            if yy > y + 33 and (xx + yy + frame) % 3 == 0: continue
            cv.put(xx, yy, SKIRT if xx < 12 + w // 2 else SKIRT_D)
    # tronco com o cardigã
    cv.rect(6, y + 14, 12, 11, CARD); cv.vline(17, y + 14, 11, CARD_D); cv.vline(6, y + 14, 11, CARD_L)
    cv.rect(10, y + 14, 4, 8, hexc("e6e2d8")); cv.vline(12, y + 15, 7, hexc("b9b2a0"))   # blusa
    cv.put(9, y + 17, hexc("d9a62e")); cv.put(9, y + 20, hexc("d9a62e"))                   # botões
    cv.put(12, y + 14, hexc("d9a62e")); cv.put(12, y + 15, hexc("d9a62e"))                 # corrente dos óculos
    # braços
    if frame == 2:  # joga um livro: braço direito para cima
        cv.rect(18, y + 6, 3, 10, CARD); cv.rect(18, y + 3, 4, 3, SK)
        cv.rect(19, y - 1 if y > 1 else 0, 5, 4, hexc("b33a2e")); cv.vline(19, 0 if y < 2 else y - 1, 4, hexc("efeadd"))
        cv.rect(3, y + 15, 3, 9, CARD); cv.rect(3, y + 24, 3, 2, SK)
    else:  # "Shhh": dedo na frente da boca (frames 0/1); caçando: braços abertos com garras
        if frame == 3:
            cv.rect(1, y + 14, 5, 3, CARD); cv.rect(18, y + 14, 5, 3, CARD)
            for x in (0, 2): cv.vline(x, y + 17, 3, SK)
            for x in (21, 23): cv.vline(x, y + 17, 3, SK)
        else:
            cv.rect(3, y + 15, 3, 9, CARD); cv.rect(3, y + 24, 3, 2, SK)
            cv.rect(15, y + 13, 3, 4, CARD); cv.rect(13, y + 10, 3, 3, SK); cv.vline(13, y + 7, 3, SK)   # mão à boca
            cv.rect(4, y + 20, 5, 6, hexc("2f5fa8")); cv.vline(4, y + 20, 6, hexc("efeadd"))       # livro debaixo do braço
    # cabeça
    cv.ellipse(12, y + 8, 6, 6.5, SK); cv.vline(17, y + 5, 6, SK_D)
    if frame == 3:  # cabelo solto, espetado
        for x in range(5, 20):
            cv.vline(x, y + 1 + (x % 3), 3 + (x % 2) * 3, HAIR)
        cv.vline(5, y + 6, 8, HAIR); cv.vline(19, y + 6, 8, HAIR)
    else:  # coque alto preso com um lápis
        cv.rect(6, y + 1, 12, 4, HAIR); cv.ellipse(12, y - 0.5 if y > 1 else 1, 4, 2.5, HAIR)
        cv.line(15, y - 2 if y > 2 else 0, 18, y + 1, hexc("e3b62a"))
        cv.vline(6, y + 5, 3, HAIR); cv.vline(17, y + 5, 3, HAIR)
    # óculos e olhos
    eye = hexc("ff3b30") if frame == 3 else hexc("ffe9a0")
    for ex in (8, 13):
        cv.frame(ex, y + 7, 4, 3, hexc("2a2b30")); cv.rect(ex + 1, y + 8, 2, 1, eye)
    cv.hline(12, y + 8, 1, hexc("2a2b30"))
    if frame == 3: cv.rect(10, y + 12, 4, 2, hexc("1b1724"))   # boca aberta
    else: cv.hline(10, y + 12, 4, hexc("6a6470"))
    cv.outline(INK)
    # um pouco transparente, como os outros fantasmas (o contorno fica firme)
    px = cv.img.load()
    for yy in range(cv.h):
        for xx in range(cv.w):
            r_, g, b, a = px[xx, yy]
            if a == 255 and (r_, g, b) != INK[:3] and yy > y + 30: px[xx, yy] = (r_, g, b, 200)
    return cv


def livros_voando():
    """Projéteis da Bibliotecária: livros abertos batendo as páginas (4 cores, sorteadas por tiro)."""
    frames = []
    for c in (hexc("b33a2e"), hexc("2f5fa8"), hexc("3f8a4a"), hexc("6a3f8f")):
        cv = Canvas(10, 8)
        cv.rect(0, 2, 4, 4, c); cv.rect(6, 2, 4, 4, c)          # capas abertas
        cv.rect(1, 1, 3, 4, hexc("efeadd")); cv.rect(6, 1, 3, 4, hexc("efeadd"))
        cv.vline(4, 1, 6, shade(c, -0.3)); cv.vline(5, 1, 6, shade(c, -0.3))
        cv.hline(1, 2, 3, hexc("b9b2a0")); cv.hline(6, 3, 3, hexc("b9b2a0"))
        cv.outline(INK)
        frames.append(cv)
    return sheet(frames, 4)


# ======================================================================== ITENS
def livro_item():
    """O livro atrasado: Cálculo I, capa vermelha com a etiqueta da biblioteca."""
    cv = Canvas(10, 12)
    cv.rect(1, 1, 8, 10, hexc("b33a2e")); cv.vline(1, 1, 10, hexc("7d241d")); cv.vline(8, 2, 9, hexc("efeadd"))
    cv.rect(3, 3, 4, 2, hexc("efe6c8")); cv.rect(3, 8, 3, 1, hexc("efe6c8"))
    cv.outline(INK)
    return cv


def chave_item(tag):
    """Chave antiga com etiqueta (amarela: Achados; vermelha: escada)."""
    cv = Canvas(12, 8)
    cv.ellipse(3, 3.5, 2.6, 2.6, hexc("d9b54a")); cv.put(3, 3, hexc("1c1a22"))
    cv.rect(5, 3, 6, 1, hexc("d9b54a")); cv.put(9, 4, hexc("d9b54a")); cv.put(10, 5, hexc("d9b54a"))
    cv.rect(1, 5, 3, 3, tag)
    cv.outline(INK)
    return cv


def livro_grande():
    """Inspeção: o Cálculo I, capa vermelha gasta, etiqueta e o carimbo de devolução atrasada."""
    W, H = 64, 84
    cv = Canvas(W, H)
    RED, RED_D, RED_L = hexc("a8352a"), hexc("6e221b"), hexc("c4544a")
    cv.rect(6, 4, 52, 76, RED); cv.rect(6, 4, 6, 76, RED_D); cv.vline(12, 4, 76, hexc("3c1310"))
    cv.rect(57, 6, 3, 72, hexc("efe8d4"))
    for y in range(8, 78, 2): cv.put(58, y, hexc("cfc6ad"))
    r = rng(3500)
    for _ in range(140):  # capa gasta
        x, y = r.randrange(13, 57), r.randrange(5, 79)
        cv.put(x, y, r.choice([RED_L, RED_D, hexc("8e2c23")]))
    text_center(cv, "CALCULO", 35, 16, hexc("e9d9a8"))
    text_center(cv, "I", 35, 24, hexc("e9d9a8"))
    cv.hline(18, 32, 34, hexc("e9d9a8"))
    cv.rect(22, 54, 26, 16, hexc("efe6c8")); cv.frame(22, 54, 26, 16, hexc("8e8466"))   # etiqueta da biblioteca
    text_center(cv, "BIB ICT", 35, 56, hexc("3a3f48"))
    text_center(cv, "515.1", 35, 63, hexc("3a3f48"))
    cv.ellipse(48, 42, 7, 7, hexc("2f5fa8", 160)); cv.ellipse(48, 42, 5, 5, RED)   # carimbo azul, borrado
    text_center(cv, "2041", 48, 40, hexc("2f5fa8", 200))
    cv.frame(6, 4, 52, 76, hexc("3c1310"))
    return cv


def chave_grande(red):
    """Inspeção: a chave dos Achados (etiqueta amarela) ou a da escada (etiqueta vermelha, com corrente)."""
    W, H = 72, 48
    cv = Canvas(W, H)
    G, GD, GL = hexc("caa548"), hexc("8e7230"), hexc("ecd27a")
    cv.ellipse(16, 18, 11, 11, G); cv.ellipse(16, 18, 5, 5, (0, 0, 0, 0))
    for yy in range(8, 30):
        for xx in range(5, 28):
            if ((xx - 16) ** 2 + (yy - 18) ** 2) < 25: cv.px[xx, yy] = (0, 0, 0, 0)
    cv.rect(26, 15, 38, 6, G); cv.hline(26, 15, 38, GL); cv.hline(26, 20, 38, GD)
    cv.rect(52, 21, 4, 7, G); cv.rect(59, 21, 4, 9, G); cv.rect(46, 21, 3, 5, G)
    tag = hexc("c7372f") if red else hexc("e3b62a")
    cv.line(10, 26, 8, 34, hexc("8a929b"))
    cv.rect(2, 34, 22, 12, tag); cv.frame(2, 34, 22, 12, shade(tag, -0.4))
    text(cv, "ESC" if red else "A/P", 6, 38, hexc("2a2b30"))
    cv.outline(INK)
    return cv


PROPS = {
    "estante0": lambda: estante(0), "estante1": lambda: estante(1), "estante2": lambda: estante(2), "estante3": lambda: estante(3),
    "mesa_estudo0": lambda: mesa_estudo(seed=0), "mesa_estudo1": lambda: mesa_estudo(seed=1),
    "mesa_sala": lambda: mesa_estudo(38, 14, seed=2),
    "mesa_redonda": mesa_redonda, "balcao_bib": balcao_bib, "caixa_devolucao": caixa_devolucao,
    "peca_peao": lambda: peca("peao"), "peca_torre": lambda: peca("torre", dark=True), "peca_cavalo": lambda: peca("cavalo"),
    "peca_rei": lambda: peca("rei", dark=True), "peca_caida": lambda: peca("peao", fallen=True),
    "peca_caida_preta": lambda: peca("torre", fallen=True, dark=True),
    "placa_molhado": placa_molhado, "armarios_azuis": armarios_azuis,
    "elevador": lambda: elevador(False), "elevador_quebrado": lambda: elevador(True), "porta_escada": porta_escada_f2,
    "cadeira_espera": lambda: cadeira_espera(False), "cadeira_espera_caida": lambda: cadeira_espera(True),
    "lousa": lousa, "armario_madeira": armario_madeira, "mesa_achados": mesa_achados,
    "caixa0": lambda: caixa_papelao(0), "caixa1": lambda: caixa_papelao(1), "caixa2": lambda: caixa_papelao(2),
    "luminaria": lambda: luminaria(True), "luminaria_apagada": lambda: luminaria(False),
}

if __name__ == "__main__":
    for name, fn in PROPS.items():
        fn().save(OUT + name + ".png")
    sheet([bibliotecaria(f) for f in range(4)], 4).save("sprites/characters/bibliotecaria.png")
    livros_voando().save("sprites/fx/livros.png")
    livro_item().save("sprites/items/livro.png")
    chave_item(hexc("e3b62a")).save("sprites/items/chave_achados.png")
    chave_item(hexc("c7372f")).save("sprites/items/chave_escada.png")
    livro_grande().save("sprites/items/inspecao/livro.png")
    chave_grande(False).save("sprites/items/inspecao/chave_achados.png")
    chave_grande(True).save("sprites/items/inspecao/chave_escada.png")
    print("fase 2 ok:", len(PROPS), "props")
