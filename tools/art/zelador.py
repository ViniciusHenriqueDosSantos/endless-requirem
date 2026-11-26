# Fantasma do zelador (mini-chefe do Hall das Caronas, Fase 2): fantasma grande de uniforme, com o boné,
# o bigode, o molho de chaves e a vassoura. Folha de 5 quadros de 26 x 30 (4 flutuando + 1 levando tiro),
# no mesmo formato dos outros fantasmas (Enemy usa o último quadro como "machucado").
# Saída: sprites/characters/fantasma_zelador.png
from pix import Canvas, sheet, shade, hexc
from palette import C

INK = C["ink"]
BODY, BODY_M, BODY_D = C["ghost"], C["ghost_m"], C["ghost_d"]
UNIF, UNIF_D = hexc("4f6fa8"), hexc("344d7d")
CAP, CAP_D = hexc("2f4f86"), hexc("1f365e")
WOOD, BRISTLE = C["wood"], hexc("d9b35a")
KEY = hexc("e2c25a")


def frame(i, hurt=False):
    w, h = 26, 30
    cv = Canvas(w, h)
    bob = [0, 1, 1, 0][i % 4]
    y0 = 5 + bob
    # corpo de fantasma (barriga larga) com a barra ondulada embaixo
    cv.ellipse(12, y0 + 10, 9, 10, BODY)
    cv.rect(3, y0 + 10, 19, 10, BODY)
    for k in range(5):
        x = 3 + k * 4
        if (k + i) % 2 == 0: cv.rect(x, y0 + 20, 3, 2, BODY)
    cv.vline(3, y0 + 8, 12, BODY_M); cv.vline(21, y0 + 8, 12, BODY_D)
    # uniforme azul (macacão) com bolso
    cv.rect(5, y0 + 13, 15, 8, UNIF); cv.hline(5, y0 + 13, 15, shade(UNIF, 0.2))
    cv.rect(10, y0 + 15, 4, 3, UNIF_D)
    cv.vline(7, y0 + 9, 4, UNIF); cv.vline(17, y0 + 9, 4, UNIF)            # alças
    # boné
    cv.rect(6, y0 - 2, 12, 4, CAP); cv.hline(6, y0 - 2, 12, shade(CAP, 0.25))
    cv.rect(13, y0 + 1, 8, 2, CAP_D)                                       # aba
    # rosto: olhos e bigode
    if hurt:
        cv.line(8, y0 + 5, 10, y0 + 7, INK); cv.line(10, y0 + 5, 8, y0 + 7, INK)
        cv.line(14, y0 + 5, 16, y0 + 7, INK); cv.line(16, y0 + 5, 14, y0 + 7, INK)
    else:
        cv.rect(8, y0 + 5, 2, 2, INK); cv.rect(14, y0 + 5, 2, 2, INK)
    cv.rect(9, y0 + 9, 6, 1, hexc("6b5a4a")); cv.put(8, y0 + 10, hexc("6b5a4a")); cv.put(15, y0 + 10, hexc("6b5a4a"))
    # vassoura na mão direita e o molho de chaves na esquerda
    cv.line(23, y0 + 1, 21, y0 + 18, WOOD)
    cv.rect(19, y0 + 19, 5, 4, BRISTLE); cv.vline(21, y0 + 19, 4, shade(BRISTLE, -0.3))
    cv.ellipse(3, y0 + 16, 1.5, 1.5, KEY); cv.put(2, y0 + 18, KEY); cv.put(4, y0 + 19, KEY)
    cv.outline(INK)
    return cv


frames = [frame(i) for i in range(4)] + [frame(0, hurt=True)]
sheet(frames, 5).save("sprites/characters/fantasma_zelador.png")
print("zelador ok")
