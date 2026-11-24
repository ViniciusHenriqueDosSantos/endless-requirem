# Props da Fase 3 (4º andar): a porta de vidro da área final, a fechadura de senha, o computador do Laboratório 2
# e o quadro de energia do Laboratório 1. 1 pixel da arte = 4 do jogo.
# Saída: sprites/props/fase3/
#   porta_vidro_fechada / porta_vidro_aberta  porta dupla de vidro com moldura de alumínio (64 x 28 = 4 tiles)
#   fechadura       teclado numérico de parede, com visor e luz (9 x 13)
#   monitor0/1      monitor em cima da bancada: apagado com cursor / aceso piscando (12 x 10)
#   quadro_energia  quadro de força aberto, fios soltos e faísca (14 x 20); quadro_energia_ok: fechado, luz verde
#   porta_vidro_estreita_fechada / _aberta  a mesma porta, com 2 tiles (32 x 28), na entrada do corredor de vidro
#   porta_lab_trancada / porta_lab_fechada / porta_lab_aberta  porta do laboratório na parede do lado (6 x 34):
#       trancada (luz vermelha), fechada mas livre (luz verde), aberta (só o batente)
#   camera0 / camera1  câmera de segurança de parede: apagada / gravando com a luz vermelha (8 x 7)
#   vidro_lateral   trecho de parede de vidro do corredor de vidro, com a noite e a chuva lá fora (16 x 16)
#   eva_azul / eva_verde   placa de tatame de EVA com os dentes de encaixe (32 x 32 = 2 x 2 tiles)
#   puff_<cor>      puff redondo para sentar no tatame (19 x 16), em várias cores
#   pingpong        mesa de ping-pong vista de cima, com a rede, raquetes e bolinha (60 x 31)
#   lousa1 / lousa2 lousa branca dos laboratórios com moldura, rabiscos de matéria e a bandeja de pincéis (160 x 16)
#   poltrona        poltrona vermelha do auditório, vista de trás (10 x 12)
#   palco           palco de madeira com a cortina no fundo (176 x 32 = 11 x 2 tiles)
#   pulpito         púlpito de madeira com o microfone (12 x 14)
#   distribuidor / distribuidor_ok  distribuidor de energia do Lab 1 (16 x 18): mostradores vermelhos / verdes
#   bancada_dupla / bancada_esq / bancada_dir  bancada comprida dos laboratórios (vista de cima, 18 x 128 = 8 tiles):
#       monitores, teclados e mouses; dupla = computadores dos dois lados, esq/dir = virados só para um lado
#   escada_palco    escadinha de madeira na lateral do palco (16 x 16)
#   diploma_palco   o diploma enrolado com fita vermelha, num suporte em cima do palco (12 x 10)
#   armario0..2     armário de aço do corredor, encostado na parede da esquerda (vista de cima, 11 x 30):
#       tampo, a frente virada para o corredor com as aberturas de ventilação, o puxador e o número; variações
#       (amassado, porta entreaberta, adesivo)
#   painel_fios     fundo da tela dos fios: chapa de metal, parafusos, réguas de bornes e adesivo de alta tensão (140 x 95)
#   painel_calibrar fundo da tela de calibrar: chapa com os três mostradores cromados e as plaquinhas (140 x 105)
from pix import Canvas, rng, shade, hexc, text, text_center
from palette import C

OUT = "sprites/props/fase3/"
INK = C["ink"]
AL, AL_L, AL_D = C["inox"], C["inox_l"], C["inox_d"]
GLASS = hexc("9fd7e8", 110)
GLASS_HI = hexc("e8fbff", 170)
GLASS_D = hexc("5e9fb5", 140)
GREEN, RED, YEL = hexc("54e38a"), hexc("e8453c"), hexc("f2d14b")
DARK, DARK_L = hexc("26292e"), hexc("3b4048")


# ======================================================================== PORTA DE VIDRO
def porta(aberta, w=64):
    """Porta dupla de vidro (vista de frente, na passagem do corredor para a área final).
    Fechada: duas folhas de vidro com reflexo e puxadores. Aberta: as folhas correram para os lados."""
    h = 28
    cv = Canvas(w, h)
    cv.rect(0, 0, w, 2, AL); cv.hline(0, 0, w, AL_L)                        # trilho de cima
    cv.rect(0, h - 2, w, 2, AL_D)                                           # soleira
    cv.rect(0, 0, 2, h, AL); cv.rect(w - 2, 0, 2, h, AL)                    # batentes
    half = (w - 4) // 2
    leaves = [(2, half // 2), (w - 2 - half // 2, half // 2)] if aberta else [(2, half), (2 + half, half)]
    for x0, lw in leaves:
        cv.rect(x0, 2, lw, h - 4, GLASS)
        cv.frame(x0, 2, lw, h - 4, AL)
        for k in range(3 if lw >= 14 else 1):                               # reflexo diagonal
            cv.line(x0 + 2 + k * 2, h - 5, x0 + min(lw - 2, 8) + k * 2, 4, GLASS_HI if k != 1 else GLASS_D)
    if not aberta:
        m = w // 2
        cv.rect(m - 3, 10, 2, 7, AL_L); cv.rect(m + 1, 10, 2, 7, AL_L)        # puxadores
        cv.rect(2 + half // 5, 13, half * 3 // 5, 3, hexc("d94a3a", 200))       # faixa "ÁREA RESTRITA"
        cv.rect(2 + half + half // 5, 13, half * 3 // 5, 3, hexc("d94a3a", 200))
    cv.outline(INK)
    return cv


# ======================================================================== FECHADURA
def fechadura():
    """Teclado de senha de parede: caixa escura, visor verde, 3x4 teclas e a luz vermelha (trancado)."""
    cv = Canvas(9, 13)
    cv.rect(0, 0, 9, 13, DARK); cv.hline(0, 0, 9, DARK_L); cv.vline(0, 0, 13, DARK_L)
    cv.rect(1, 1, 7, 2, hexc("173d2a")); cv.hline(2, 2, 3, GREEN)            # visor
    for j in range(4):
        for i in range(3):
            cv.put(2 + i * 2, 4 + j * 2, hexc("c9ccd2") if (i + j) % 3 else hexc("e8eaee"))
    cv.put(7, 11, RED)                                                        # luz
    cv.outline(INK)
    return cv


# ======================================================================== MONITOR
def monitor(aceso):
    """Monitor em cima da bancada. Apagado: tela escura com o cursor. Aceso: tela azul clara com o aviso."""
    cv = Canvas(12, 10)
    cv.rect(0, 0, 12, 8, DARK); cv.hline(0, 0, 12, DARK_L)
    cv.rect(1, 1, 10, 6, hexc("63d0ff") if aceso else hexc("0d1a2b"))
    if aceso:
        cv.hline(2, 2, 6, hexc("e8fbff")); cv.hline(2, 4, 4, hexc("e8fbff")); cv.put(9, 5, YEL)
    else:
        cv.hline(2, 5, 2, GREEN)                                              # cursor
    cv.rect(5, 8, 2, 1, DARK_L); cv.rect(3, 9, 6, 1, DARK)                   # pé
    cv.outline(INK)
    return cv


# ======================================================================== QUADRO DE ENERGIA
def quadro(ok):
    """Quadro de força na parede. Quebrado: porta aberta, fios coloridos soltos e uma faísca. Consertado: fechado."""
    cv = Canvas(14, 20)
    cv.rect(0, 0, 14, 20, C["steel"]); cv.hline(0, 0, 14, C["steel_l"]); cv.hline(0, 19, 14, C["steel_d"])
    if ok:
        cv.frame(1, 1, 12, 18, C["steel_d"])
        cv.rect(10, 9, 1, 3, C["steel_k"])                                    # trinco
        cv.put(3, 3, GREEN); cv.put(4, 3, GREEN)
        cv.rect(4, 13, 6, 3, YEL); cv.put(6, 14, INK); cv.put(7, 14, INK)     # placa de perigo
    else:
        cv.rect(1, 1, 12, 18, hexc("1c1e22"))                                 # interior
        for i, c in enumerate([RED, hexc("3f7fe0"), YEL, hexc("e070c8")]):
            x = 2 + i * 3
            cv.vline(x, 2, 6 + i % 2 * 3, c)                                   # fios pendurados, soltos
            cv.put(x + 1, 8 + i % 2 * 3, c)
        r = rng(77)
        for _ in range(5):
            cv.put(r.randrange(3, 11), r.randrange(12, 18), hexc("fff3a0"))  # faíscas
        cv.rect(13, 1, 1, 18, C["steel_l"])                                   # porta aberta (de lado)
    cv.outline(INK)
    return cv


# ======================================================================== PORTA DO LABORATÓRIO
def porta_lab(estado):
    """Porta na parede do lado (vista de cima): a folha de madeira em pé, com visor de vidro e a luz da tranca.
    estado: "trancada" (luz vermelha), "fechada" (luz verde, abre quando o jogador chega) ou "aberta"."""
    w, h = 6, 34
    cv = Canvas(w, h)
    cv.rect(0, 0, w, 2, C["steel"]); cv.rect(0, h - 2, w, 2, C["steel_d"])     # batente em cima e embaixo
    if estado == "aberta":
        cv.rect(1, 2, 1, h - 4, hexc("1c1e22", 160))                           # vão escuro, a folha encostada
        cv.rect(4, 2, 2, 10, C["wood_d"])
    else:
        cv.rect(1, 2, 4, h - 4, C["wood"]); cv.vline(1, 2, h - 4, C["wood_l"]); cv.vline(4, 2, h - 4, C["wood_d"])
        cv.rect(2, 8, 2, 8, hexc("9fd7e8"))                                     # visor
        cv.put(3, 20, C["steel_l"])                                             # maçaneta
        cv.rect(2, 3, 2, 2, RED if estado == "trancada" else GREEN)             # luz da tranca
    cv.outline(INK)
    return cv


# ======================================================================== CÂMERA
def camera(gravando):
    """Câmera de segurança presa na parede: corpo branco, lente escura e a luz (vermelha quando grava)."""
    cv = Canvas(8, 7)
    cv.rect(0, 0, 2, 4, C["steel_d"])                                           # suporte na parede
    cv.rect(2, 1, 5, 4, hexc("e4e6ea")); cv.hline(2, 1, 5, hexc("ffffff")); cv.hline(2, 4, 5, hexc("a9adb5"))
    cv.rect(6, 2, 2, 2, hexc("1d1e22"))                                         # lente
    cv.put(3, 2, RED if gravando else hexc("5a2622"))
    cv.outline(INK)
    return cv


# ======================================================================== PAREDE DE VIDRO
def vidro_lateral():
    """Trecho da parede de vidro do corredor (vista de cima): moldura de alumínio, a noite azul lá fora e a chuva."""
    cv = Canvas(16, 16)
    r = rng(91)
    cv.rect(3, 0, 10, 16, hexc("15223d"))                                       # noite lá fora
    for _ in range(9):
        x, y = r.randrange(4, 12), r.randrange(0, 14)
        cv.put(x, y, hexc("6f8fc7")); cv.put(x, y + 1, hexc("44608f"))         # gotas de chuva no vidro
    cv.rect(3, 0, 10, 16, hexc("9fd7e8", 60))                                   # o vidro
    cv.line(5, 13, 9, 3, hexc("e8fbff", 120))                                   # reflexo
    cv.rect(2, 0, 1, 16, AL); cv.rect(13, 0, 1, 16, AL_D)                       # moldura
    cv.rect(2, 15, 12, 1, AL)
    return cv


# ======================================================================== TATAME E PUFFS
def eva(base):
    """Placa de EVA (2x2 tiles): cor lisa com textura de pontinhos, borda mais escura e os dentes de encaixe."""
    cv = Canvas(32, 32)
    r = rng(sum(base[:3]))
    dark, light = shade(base, -0.25), shade(base, 0.18)
    cv.rect(0, 0, 32, 32, base)
    for _ in range(40):
        cv.put(r.randrange(1, 31), r.randrange(1, 31), light if r.random() < 0.5 else shade(base, -0.1))
    cv.frame(0, 0, 32, 32, dark)
    for k in range(4):                                       # dentes de encaixe nas quatro bordas
        a = 3 + k * 7
        cv.rect(a, 0, 3, 2, dark); cv.rect(a + 3, 30, 3, 2, dark)
        cv.rect(0, a + 3, 2, 3, dark); cv.rect(30, a, 2, 3, dark)
    return cv


def puff(base):
    """Puff redondo (pufe de bolinhas de isopor) visto de cima/de lado, com a dobra do tecido e o brilho."""
    cv = Canvas(19, 16)
    cv.ellipse(9, 10, 9, 5, shade(base, -0.35))              # base mais escura (lado)
    cv.ellipse(9, 7, 9, 6.5, base)
    cv.ellipse(7, 5, 4.5, 3, shade(base, 0.2))               # brilho
    cv.line(5, 9, 13, 7, shade(base, -0.2))                  # dobra de quem sentou
    cv.line(9, 2, 10, 6, shade(base, -0.12))                 # costura
    cv.outline(INK)
    return cv


# ======================================================================== PING-PONG
def pingpong():
    """Mesa de ping-pong (vista 3/4): tampo verde com bordas e linha brancas, a rede no meio, pés e as raquetes."""
    w, h = 60, 31
    cv = Canvas(w, h)
    top, side = hexc("1f6b4a"), hexc("124430")
    white = hexc("eef2ee")
    cv.rect(2, 1, w - 4, 22, top)
    cv.frame(2, 1, w - 4, 22, white)
    cv.hline(3, 11, w - 6, white)                            # linha do meio
    cv.rect(2, 23, w - 4, 3, side)                           # grossura do tampo
    for x in (5, w - 7, w // 2 - 1):                         # pés
        cv.rect(x, 26, 2, 4, C["steel_d"])
    cv.rect(w // 2 - 1, 0, 2, 24, hexc("d8dcd8")); cv.vline(w // 2, 0, 24, hexc("9aa09a"))   # rede
    cv.rect(w // 2 - 2, 0, 4, 1, C["steel_k"])
    cv.ellipse(12, 7, 3, 2, hexc("c8322b")); cv.rect(14, 8, 3, 1, C["wood"])                    # raquete vermelha
    cv.ellipse(46, 15, 3, 2, hexc("1d1e22")); cv.rect(42, 16, 3, 1, C["wood"])                  # raquete preta
    cv.put(22, 15, hexc("ffffff")); cv.put(22, 16, hexc("f29a2e"))                              # bolinha
    cv.outline(INK)
    return cv


# ======================================================================== LOUSA
def lousa(variant):
    """Lousa branca de laboratório: moldura de alumínio, rabiscos de pincel azul/vermelho e a bandeja com pincéis."""
    w, h = 160, 16
    cv = Canvas(w, h)
    r = rng(400 + variant)
    blue, red, black = hexc("2f5fd0"), hexc("d03a32"), hexc("2b2d30")
    cv.rect(0, 0, w, h - 2, hexc("f6f7f9"))
    for _ in range(25):                                       # manchas de apagador
        x, y = r.randrange(2, w - 8), r.randrange(1, h - 4)
        cv.hline(x, y, r.randrange(3, 8), hexc("e3e6ea"))
    if variant == 1:
        text(cv, "ESTRUTURA DE DADOS", 4, 2, blue)
        text(cv, "ARVORE BINARIA", 4, 8 - 1, black)
        for (x0, y0, x1, y1) in [(96, 3, 90, 7), (96, 3, 102, 7), (90, 7, 87, 9), (102, 7, 105, 9)]:
            cv.line(x0, y0, x1, y1, black)
        for (x, y) in [(96, 2), (90, 7), (102, 7), (87, 9), (105, 9)]:
            cv.rect(x - 1, y - 1, 2, 2, red)
        text(cv, "LOG N", 116, 3, red)
    else:
        text(cv, "CALCULO I", 4, 2, blue)
        text(cv, "PROVA SEXTA!", 4, 8 - 1, red)
        for x in range(60, 110):                              # curva de uma função
            y = int(6 - 3.5 * __import__("math").sin((x - 60) / 7))
            cv.put(x, y, black)
        cv.hline(58, 6, 54, hexc("9aa0a8")); cv.vline(60, 1, 9, hexc("9aa0a8"))
        text(cv, "SEN X", 120, 3, blue)
    cv.frame(0, 0, w, h - 2, C["inox"])
    cv.rect(2, h - 2, w - 4, 2, C["inox_d"])                  # bandeja
    cv.rect(20, h - 3, 6, 1, blue); cv.rect(28, h - 3, 6, 1, red); cv.rect(36, h - 3, 6, 1, black)
    cv.rect(130, h - 4, 8, 2, hexc("3b4048"))                 # apagador
    cv.outline(INK)
    return cv


# ======================================================================== AUDITÓRIO
def poltrona():
    """Poltrona do auditório vista de trás (a plateia olha para o palco, embaixo): encosto vermelho e braços."""
    cv = Canvas(10, 12)
    red, red_d, red_l = hexc("a3262c"), hexc("6e1418"), hexc("c94046")
    cv.rect(1, 0, 8, 8, red); cv.hline(1, 0, 8, red_l); cv.vline(1, 0, 8, red_l)   # encosto
    cv.rect(2, 2, 6, 1, red_d)                                                     # costura
    cv.rect(0, 5, 1, 5, hexc("3b2a22")); cv.rect(9, 5, 1, 5, hexc("3b2a22"))       # braços
    cv.rect(1, 8, 8, 2, red_d)                                                     # assento
    cv.rect(2, 10, 1, 2, C["steel_d"]); cv.rect(7, 10, 1, 2, C["steel_d"])        # pés
    cv.outline(INK)
    return cv


def palco():
    """Palco de madeira (11 x 2 tiles): tábuas, borda da frente mais clara e a cortina vermelha no fundo (embaixo)."""
    w, h = 176, 32
    cv = Canvas(w, h)
    r = rng(707)
    for y in range(0, 22, 3):                                # tábuas
        cv.rect(0, y, w, 3, C["wood"] if (y // 3) % 2 else C["wood_l"])
        cv.hline(0, y + 2, w, C["wood_d"])
        for _ in range(6):
            x = r.randrange(0, w); cv.vline(x, y, 3, C["wood_d"])
    cv.hline(0, 0, w, hexc("e8c98f"))                        # quina da frente do palco
    curtain, curtain_d = hexc("8e1a22"), hexc("5b0f15")
    for x in range(w):                                        # cortina pregueada no fundo
        cv.vline(x, 22, 10, curtain if (x // 3) % 2 else curtain_d)
    cv.hline(0, 22, w, hexc("d4a93a"))                       # barra dourada
    cv.outline(INK)
    return cv


def pulpito():
    """Púlpito de madeira com o microfone."""
    cv = Canvas(12, 14)
    cv.rect(1, 4, 10, 10, C["wood"]); cv.vline(1, 4, 10, C["wood_l"]); cv.vline(10, 4, 10, C["wood_d"])
    cv.rect(0, 3, 12, 2, C["wood_d"])                        # tampo inclinado
    cv.rect(4, 7, 4, 3, hexc("d4a93a"))                      # brasão
    cv.line(7, 3, 9, 0, C["steel_k"]); cv.put(9, 0, hexc("1d1e22"))   # microfone
    cv.outline(INK)
    return cv


# ======================================================================== DISTRIBUIDOR
def distribuidor(ok):
    """Armário do distribuidor de energia (encostado na parede de baixo): três mostradores redondos e a faixa
    de perigo. Descalibrado: ponteiros vermelhos tortos. Calibrado: verdes e alinhados."""
    cv = Canvas(16, 18)
    cv.rect(0, 0, 16, 18, C["steel"]); cv.hline(0, 0, 16, C["steel_l"]); cv.rect(0, 16, 16, 2, C["steel_d"])
    for i in range(3):
        cy = 3 + i * 4
        cv.ellipse(5, cy + 1, 2.2, 1.8, hexc("1d1e22"))
        cv.put(5, cy + 1, GREEN if ok else RED)
        cv.put(5 + (0 if ok else 1), cy, GREEN if ok else RED)
        cv.rect(9, cy, 5, 2, hexc("3b4048")); cv.rect(9, cy, 2 + (3 if ok else i), 2, GREEN if ok else YEL)   # barrinha
    for x in range(16):                                                         # faixa de perigo
        cv.put(x, 15, YEL if (x // 2) % 2 == 0 else INK)
    cv.outline(INK)
    return cv


# ======================================================================== BANCADA DO LABORATÓRIO
def bancada(kind, seed):
    """Bancada comprida (vista de cima): tampo de fórmica cinza claro, com um computador por cadeira.
    Cada computador: monitor de lado (fino, com a tela virada para a cadeira), teclado e mouse na frente.
    Alguns monitores estão acesos, outros apagados, e um ou outro lugar está vazio (laboratório abandonado)."""
    w, h = 18, 128
    cv = Canvas(w, h)
    r = rng(900 + seed)
    top, top_d, top_l = hexc("c9c4b8"), hexc("9e988b"), hexc("dedad0")
    cv.rect(1, 0, w - 2, h, top); cv.vline(1, 0, h, top_l); cv.vline(w - 2, 0, h, top_d)
    for _ in range(30):                                                     # riscos e manchas do tampo
        cv.put(r.randrange(2, w - 2), r.randrange(0, h), hexc("bdb7aa"))
    sides = {"dupla": ("E", "D"), "esq": ("E",), "dir": ("D",)}[kind]
    for slot in range(4):
        y = 14 + slot * 32                                                  # na altura de cada cadeira
        for side in sides:
            if r.random() < 0.1: continue                                   # lugar vazio
            lit = r.random() < 0.35
            screen = hexc("63d0ff") if lit else hexc("16243a")
            if kind == "dupla":
                mx = 7 if side == "E" else 10                               # monitores de costas um pro outro
            else:
                mx = 10 if side == "E" else 6
            face = -1 if side == "E" else 1                                  # para que lado a tela olha
            cv.rect(mx, y - 7, 2, 14, hexc("2b2d30"))                        # monitor de lado
            cv.vline(mx + (1 if face > 0 else 0) + face, y - 6, 12, screen)  # a tela (borda virada pra cadeira)
            cv.rect(mx - (1 if face < 0 else 0), y - 1, 2, 3, hexc("3b4048")) # pé
            kx = mx + face * 4 if face > 0 else mx - 5
            kx = max(2, min(w - 5, kx))
            cv.rect(kx, y - 4, 3, 8, hexc("e7e8ea"))                         # teclado
            for k in range(3):
                cv.put(kx + 1, y - 3 + k * 3, hexc("b8bcc2"))
            cv.put(kx + (1 if face > 0 else 1), y + 6, hexc("e7e8ea"))      # mouse
        if r.random() < 0.5:                                                 # fio solto
            cv.line(w // 2, y + 8, w // 2 + r.choice((-2, 2)), y + 12, hexc("1d1e22"))
    cv.outline(INK)
    return cv


# ======================================================================== FUNDOS DAS TELAS DE ENERGIA
def chapa(cv, r):
    """Chapa de metal escovado com degradê, parafusos nos cantos e sujeira."""
    w, h = cv.w, cv.h
    for y in range(h):
        base = shade(hexc("6d737b"), 0.12 - 0.25 * y / h)
        cv.hline(0, y, w, base)
        for x in range(0, w, 3):
            if r.random() < 0.25: cv.put(x + r.randrange(0, 3), y, shade(base, 0.08))   # escovado
    for _ in range(60):
        cv.put(r.randrange(0, w), r.randrange(0, h), hexc("4b4f55"))                    # sujeira
    cv.frame(0, 0, w, h, hexc("2b2d30")); cv.frame(1, 1, w - 2, h - 2, hexc("9aa0a8"))
    for x, y in ((4, 4), (w - 5, 4), (4, h - 5), (w - 5, h - 5)):                       # parafusos
        cv.ellipse(x, y, 2, 2, hexc("c9cdd3")); cv.put(x - 1, y, hexc("5a5f66")); cv.put(x + 1, y, hexc("5a5f66"))


def adesivo(cv, x, y):
    """Adesivo amarelo de alta tensão (triângulo com raio)."""
    cv.rect(x, y, 34, 9, YEL); cv.frame(x, y, 34, 9, INK)
    for i in range(4):
        cv.hline(x + 4 - i, y + 7 - i * 2, 1 + i * 2, INK)
    cv.put(x + 4, y + 3, YEL); cv.put(x + 4, y + 5, YEL)
    text(cv, "PERIGO", x + 9, y + 2, INK)


def faixa(cv, x, y, w):
    """Faixa zebrada amarela e preta de perigo."""
    for k in range(w):
        for j in range(4):
            cv.put(x + k, y + j, YEL if ((k + j) // 3) % 2 == 0 else INK)


def painel_fios():
    """Fundo da tela dos fios (140 x 95 = 560 x 380): régua de bornes dos dois lados, canaleta, adesivo."""
    cv = Canvas(140, 95)
    r = rng(1200)
    chapa(cv, r)
    for x0 in (4, 118):                                                   # réguas de bornes
        cv.rect(x0, 8, 18, 79, hexc("26282c")); cv.frame(x0, 8, 18, 79, hexc("111214"))
        for i in range(4):
            y = int(17.5 + i * 20.5)
            cv.rect(x0 + 2, y - 5, 14, 10, hexc("3a3d42"))
            cv.ellipse(x0 + 9, y, 3, 3, hexc("b9bdc4")); cv.hline(x0 + 7, y, 5, hexc("5a5f66"))   # parafuso do borne
    cv.rect(30, 4, 80, 5, hexc("8d939b")); cv.frame(30, 4, 80, 5, hexc("4b4f55"))              # canaleta
    adesivo(cv, 72, 82)
    cv.rect(30, 82, 30, 9, hexc("e8eaee")); cv.frame(30, 82, 30, 9, INK); text(cv, "QDF-L1", 32, 84, INK)  # etiqueta
    for i in range(6):                                                    # disjuntores pequenos no alto
        cv.rect(34 + i * 12, 12, 8, 12, hexc("e4e6ea")); cv.frame(34 + i * 12, 12, 8, 12, INK)
        cv.rect(37 + i * 12, 15 + (i % 2) * 4, 2, 3, hexc("3b4048"))
    return cv


def painel_calibrar():
    """Fundo da tela de calibrar (140 x 105 = 560 x 420): três aros cromados para os mostradores e plaquinhas."""
    cv = Canvas(140, 105)
    r = rng(1300)
    chapa(cv, r)
    for i in range(3):
        cx, cy = 27.5, 20 + i * 32.5
        cv.ellipse(cx, cy, 16, 16, hexc("2b2d30"))
        cv.ellipse(cx, cy, 15, 15, hexc("d9dde3"))                          # aro cromado
        cv.ellipse(cx - 1, cy - 1, 14, 14, hexc("f4f6f8"))
        cv.ellipse(cx, cy, 13.5, 13.5, hexc("101114"))
        cv.rect(48, int(cy) - 4, 28, 9, hexc("e8eaee")); cv.frame(48, int(cy) - 4, 28, 9, INK)
        text(cv, f"FASE {i + 1}", 50, int(cy) - 2, INK)
        cv.put(80, int(cy), RED)                                           # luz de cada fase
    faixa(cv, 0, 100, 140)
    adesivo(cv, 100, 89)
    return cv


bancada("dupla", 1).save(OUT + "bancada_dupla.png")
bancada("esq", 2).save(OUT + "bancada_esq.png")
bancada("dir", 3).save(OUT + "bancada_dir.png")
painel_fios().save(OUT + "painel_fios.png")
painel_calibrar().save(OUT + "painel_calibrar.png")
# ======================================================================== ARMÁRIO DO CORREDOR
def armario(variant):
    """Armário de aço azul (2 tiles de altura) encostado na parede da esquerda: de cima se vê o tampo e,
    à direita, a frente com duas portas (ventilação, puxador e número). variant muda os detalhes."""
    w, h = 11, 30
    cv = Canvas(w, h)
    r = rng(1500 + variant)
    blue, blue_l, blue_d, blue_k = hexc("3d6a9e"), hexc("5a86ba"), hexc("2b4c74"), hexc("1d3350")
    cv.rect(0, 0, 7, h, blue_l); cv.vline(0, 0, h, hexc("7aa2cf"))                     # tampo (visto de cima)
    cv.rect(7, 0, 4, h, blue); cv.vline(10, 0, h, blue_d)                              # frente (virada pro corredor)
    cv.hline(7, h // 2, 4, blue_k)                                                     # divisão das duas portas
    for door in range(2):
        y0 = 2 + door * 15
        for k in range(3):                                                             # aberturas de ventilação
            cv.hline(8, y0 + k * 2, 2, blue_k)
        cv.rect(8, y0 + 8, 1, 3, hexc("c9cdd3"))                                        # puxador
        cv.put(9, y0 + 11, hexc("e8eaee"))                                              # plaquinha do número
    if variant == 1:                                                                   # amassado
        cv.rect(2, 9, 3, 2, shade(blue_l, -0.15)); cv.put(3, 10, blue_d)
    if variant == 2:                                                                   # porta entreaberta
        cv.rect(9, 16, 2, 12, hexc("1a1c20")); cv.vline(10, 16, 12, blue)
    if variant != 1:                                                                   # adesivo colado no tampo
        cv.rect(2, 20 - variant * 4, 3, 3, [hexc("e8c22e"), hexc("e05a8a"), hexc("54e38a")][variant])
    for _ in range(8):                                                                 # arranhões
        cv.put(r.randrange(1, 6), r.randrange(1, h - 1), hexc("6f96c4"))
    cv.outline(INK)
    return cv


for v in range(3):
    armario(v).save(OUT + f"armario{v}.png")


def escada_palco():
    """Escadinha de madeira (vista de cima) que sobe da plateia para o palco: degraus de baixo para cima."""
    cv = Canvas(16, 16)
    for i in range(4):
        y = i * 4
        cv.rect(0, y, 16, 4, shade(C["wood"], 0.15 - i * 0.08)); cv.hline(0, y + 3, 16, C["wood_d"])
    cv.vline(0, 0, 16, C["steel_d"]); cv.vline(15, 0, 16, C["steel_d"])          # corrimão dos lados
    cv.outline(INK)
    return cv


def diploma_palco():
    """O diploma: canudo de papel enrolado com fita vermelha, num suporte dourado, brilhando."""
    cv = Canvas(12, 10)
    cv.rect(2, 7, 8, 3, hexc("b08a2e")); cv.hline(2, 7, 8, hexc("e2c25a"))       # suporte
    cv.rect(1, 3, 10, 4, hexc("f4ecd6")); cv.hline(1, 3, 10, hexc("ffffff")); cv.hline(1, 6, 10, hexc("cfc4a6"))
    cv.vline(5, 3, 4, hexc("c8322b")); cv.vline(6, 3, 4, hexc("c8322b"))          # fita
    cv.put(4, 7, hexc("c8322b")); cv.put(7, 7, hexc("c8322b"))
    cv.put(10, 1, hexc("fff6a0")); cv.put(1, 0, hexc("fff6a0"))                   # brilho
    cv.outline(INK)
    return cv


escada_palco().save(OUT + "escada_palco.png")
diploma_palco().save(OUT + "diploma_palco.png")
distribuidor(False).save(OUT + "distribuidor.png")
distribuidor(True).save(OUT + "distribuidor_ok.png")
eva(hexc("2d5d95")).save(OUT + "eva_azul.png")
eva(hexc("2e7a4e")).save(OUT + "eva_verde.png")
for name, color in [("vermelho", "d1453b"), ("amarelo", "e8be2e"), ("roxo", "8a4fc9"), ("laranja", "ec8a2f"),
                    ("rosa", "e66aa8"), ("turquesa", "2fb3a8"), ("azul", "3f7fe0"), ("verde", "5cc05a"),
                    ("cinza", "8a8f98")]:
    puff(hexc(color)).save(OUT + f"puff_{name}.png")
pingpong().save(OUT + "pingpong.png")
lousa(1).save(OUT + "lousa1.png")
lousa(2).save(OUT + "lousa2.png")
poltrona().save(OUT + "poltrona.png")
palco().save(OUT + "palco.png")
pulpito().save(OUT + "pulpito.png")
porta(False).save(OUT + "porta_vidro_fechada.png")
porta(False, 32).save(OUT + "porta_vidro_estreita_fechada.png")
porta(True, 32).save(OUT + "porta_vidro_estreita_aberta.png")
for estado in ("trancada", "fechada", "aberta"):
    porta_lab(estado).save(OUT + f"porta_lab_{estado}.png")
camera(False).save(OUT + "camera0.png")
camera(True).save(OUT + "camera1.png")
vidro_lateral().save(OUT + "vidro_lateral.png")
porta(True).save(OUT + "porta_vidro_aberta.png")
fechadura().save(OUT + "fechadura.png")
monitor(False).save(OUT + "monitor0.png")
monitor(True).save(OUT + "monitor1.png")
quadro(False).save(OUT + "quadro_energia.png")
quadro(True).save(OUT + "quadro_energia_ok.png")
