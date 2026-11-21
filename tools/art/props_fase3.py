# Props da Fase 3 (4º andar): a porta de vidro da área final, a fechadura de senha, o computador do Laboratório 2
# e o quadro de energia do Laboratório 1. 1 pixel da arte = 4 do jogo.
# Saída: sprites/props/fase3/
#   porta_vidro_fechada / porta_vidro_aberta  porta dupla de vidro com moldura de alumínio (64 x 28 = 4 tiles)
#   fechadura       teclado numérico de parede, com visor e luz (9 x 13)
#   monitor0/1      monitor em cima da bancada: apagado com cursor / aceso piscando (12 x 10)
#   quadro_energia  quadro de força aberto, fios soltos e faísca (14 x 20); quadro_energia_ok: fechado, luz verde
from pix import Canvas, rng, shade, hexc, text_center
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
def porta(aberta):
    """Porta dupla de vidro (vista de frente, na passagem do corredor para a área final).
    Fechada: duas folhas de vidro com reflexo e puxadores. Aberta: as folhas correram para os lados."""
    w, h = 64, 28
    cv = Canvas(w, h)
    cv.rect(0, 0, w, 2, AL); cv.hline(0, 0, w, AL_L)                        # trilho de cima
    cv.rect(0, h - 2, w, 2, AL_D)                                           # soleira
    cv.rect(0, 0, 2, h, AL); cv.rect(w - 2, 0, 2, h, AL)                    # batentes
    leaves = [(2, 14), (48, 14)] if aberta else [(2, 30), (32, 30)]
    for x0, lw in leaves:
        cv.rect(x0, 2, lw, h - 4, GLASS)
        cv.frame(x0, 2, lw, h - 4, AL)
        for k in range(3):                                                  # reflexo diagonal
            cv.line(x0 + 3 + k * 2, h - 5, x0 + 9 + k * 2, 4, GLASS_HI if k != 1 else GLASS_D)
    if not aberta:
        cv.rect(29, 10, 2, 7, AL_L); cv.rect(33, 10, 2, 7, AL_L)              # puxadores
        cv.rect(8, 13, 18, 3, hexc("d94a3a", 200))                             # faixa "ÁREA RESTRITA"
        cv.rect(38, 13, 18, 3, hexc("d94a3a", 200))
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


porta(False).save(OUT + "porta_vidro_fechada.png")
porta(True).save(OUT + "porta_vidro_aberta.png")
fechadura().save(OUT + "fechadura.png")
monitor(False).save(OUT + "monitor0.png")
monitor(True).save(OUT + "monitor1.png")
quadro(False).save(OUT + "quadro_energia.png")
quadro(True).save(OUT + "quadro_energia_ok.png")
