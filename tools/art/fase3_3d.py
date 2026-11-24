# Texturas da cena em 3D do auditório (Fase 3): pixel art pequena, aplicada nos blocos 3D com filtro "nearest".
# Saída: sprites/3d/
#   madeira   tábuas do chão e do palco (32 x 32)        parede   painel acústico da parede (32 x 32)
#   carpete   tapete vermelho do corredor (16 x 16)        poltrona tecido vermelho das poltronas (16 x 16)
#   tela_off  o telão desligado (64 x 36)                  sia      o rosto da S.I.A. no telão (64 x 36)
#   diploma   o papel do diploma (32 x 16)                 cortina  cortina vermelha (16 x 32)
from pix import Canvas, rng, shade, hexc, text, text_center
from palette import C

OUT = "sprites/3d/"


def madeira():
    cv = Canvas(32, 32); r = rng(1)
    for y in range(0, 32, 4):
        base = shade(hexc("8a5a34"), r.uniform(-0.08, 0.08))
        cv.rect(0, y, 32, 4, base); cv.hline(0, y + 3, 32, shade(base, -0.3))
        x = r.randrange(0, 32); cv.vline(x, y, 3, shade(base, -0.3))
        for _ in range(6): cv.put(r.randrange(0, 32), y + r.randrange(0, 3), shade(base, 0.12))
    return cv


def parede():
    cv = Canvas(32, 32); r = rng(2)
    cv.rect(0, 0, 32, 32, hexc("5b4a40"))
    for x in range(0, 32, 8):
        cv.vline(x, 0, 32, hexc("3e3129"))
        for _ in range(20): cv.put(x + r.randrange(1, 8), r.randrange(0, 32), hexc("665448"))
    cv.hline(0, 31, 32, hexc("3e3129"))
    return cv


def carpete():
    cv = Canvas(16, 16); r = rng(3)
    cv.rect(0, 0, 16, 16, hexc("7a1a20"))
    for _ in range(40): cv.put(r.randrange(0, 16), r.randrange(0, 16), hexc("8f2229") if r.random() < 0.6 else hexc("61141a"))
    return cv


def poltrona():
    cv = Canvas(16, 16); r = rng(4)
    cv.rect(0, 0, 16, 16, hexc("a3262c"))
    for _ in range(30): cv.put(r.randrange(0, 16), r.randrange(0, 16), hexc("8e1f25"))
    cv.hline(0, 0, 16, hexc("c94046"))
    return cv


def cortina():
    cv = Canvas(16, 32)
    for x in range(16):
        cv.vline(x, 0, 32, hexc("8e1a22") if (x // 2) % 2 else hexc("5b0f15"))
    return cv


def tela(on):
    w, h = 64, 36
    cv = Canvas(w, h); r = rng(5)
    cv.rect(0, 0, w, h, hexc("05070c"))
    for y in range(0, h, 2): cv.hline(0, y, w, hexc("080b12"))           # linhas de varredura
    if not on:
        return cv
    glow, core = hexc("1f9fd6"), hexc("aef3ff")
    for side in (-1, 1):                                                    # dois olhos
        cx = 32 + side * 12
        for y in range(10, 20):
            for x in range(cx - 7, cx + 8):
                d = ((x - cx) / 7) ** 2 + ((y - 15) / 4.5) ** 2
                if d < 1: cv.put(x, y, glow if d > 0.35 else core)
        cv.rect(cx - 1, 13, 3, 4, hexc("05070c"))                           # pupila
    for x in range(18, 47):                                                 # boca (onda digital)
        y = 27 + (1 if (x // 3) % 2 else -1)
        cv.put(x, y, glow); cv.put(x, y + 1, glow)
    for _ in range(40):                                                     # ruído
        cv.put(r.randrange(0, w), r.randrange(0, h), hexc("123044"))
    text_center(cv, "S.I.A.", 32, 2, hexc("3fb6e0"))
    return cv


def diploma():
    cv = Canvas(32, 16)
    cv.rect(0, 0, 32, 16, hexc("f4ecd6")); cv.frame(1, 1, 30, 14, hexc("c9a34a"))
    for y in (5, 8, 11): cv.hline(6, y, 20, hexc("b7ab8e"))
    cv.rect(14, 2, 4, 2, hexc("6b4a1a"))
    cv.rect(25, 10, 4, 4, hexc("c8322b"))                                   # selo
    return cv


madeira().save(OUT + "madeira.png")
parede().save(OUT + "parede.png")
carpete().save(OUT + "carpete.png")
poltrona().save(OUT + "poltrona.png")
cortina().save(OUT + "cortina.png")
tela(False).save(OUT + "tela_off.png")
tela(True).save(OUT + "sia.png")
diploma().save(OUT + "diploma.png")
