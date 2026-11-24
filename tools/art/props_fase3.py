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
