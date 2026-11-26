# Logotipo do jogo "Instituto de Ciência e Terror" (menu principal) e o título "PAUSADO" (menu de pausa).
# Saída: sprites/ui/menu/logo.png e sprites/ui/menu/logo_pausado.png (pixel art; o menu amplia ×2,4 como o cenário).
#   linha de cima: "INSTITUTO DE CIÊNCIA" pequeno, creme, com um frasco de laboratório borbulhando
#   linha de baixo: palavra grande em vermelho com sangue escorrendo e um fantasma espiando
import os
import random
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.join(os.path.dirname(__file__), "..", "..")
OUT = os.path.join(ROOT, "sprites", "ui", "menu")
FONT = os.path.join(ROOT, "fonts", "PixelifySans.ttf")          # palavra grande
SMALL_FONT = os.path.join(ROOT, "fonts", "PressStart2P.ttf")   # linha de cima (o Ê fica nítido)


def rgba(h, a=255):
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


INK = rgba("1c1a22")
CREAM, CREAM_D = rgba("e3ddd1"), rgba("a9a191")
RED_HI, RED, RED_D, RED_K = rgba("e8604f"), rgba("c7372f"), rgba("93412f"), rgba("5e2a23")
GREEN, GREEN_L = rgba("2bb673"), rgba("8ff0b8")
GLASS = rgba("bccbd8")


def text_mask(text, size, spacing=0, font_path=None):
    """Máscara (numpy bool) do texto em pixel art, sem suavização."""
    font = ImageFont.truetype(font_path or FONT, size)
    widths = [int(round(font.getlength(ch))) for ch in text]   # avanço real de cada letra (inclui o espaço)
    w = sum(widths) + spacing * (len(text) - 1) + 8
    im = Image.new("L", (w, size * 2), 0)
    d = ImageDraw.Draw(im)
    d.fontmode = "1"
    x = 2
    for ch, cw in zip(text, widths):
        d.text((x, 2), ch, font=font, fill=255)
        x += cw + spacing
    m = np.array(im) > 127
    ys, xs = np.nonzero(m)
    return m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def bold(m):
    out = m.copy()
    out[:, 1:] |= m[:, :-1]
    return out


def outline(m, diagonal=True):
    pad = np.pad(m, 1)
    o = np.zeros_like(pad)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if (dx or dy) and (diagonal or not (dx and dy)):
                o |= np.roll(np.roll(pad, dy, 0), dx, 1)
    return o & ~pad, pad


def paint(canvas, mask, x, y, color):
    ys, xs = np.nonzero(mask)
    for yy, xx in zip(ys, xs):
        canvas.putpixel((x + xx, y + yy), color)


def big_word(word, size, seed):
    """Palavra grande vermelha: degradê, brilho em cima, sangue escorrendo, contorno e sombra."""
    rng = random.Random(seed)
    m = bold(text_mask(word, size, spacing=2))
    h, w = m.shape
    drip_room = 10
    body = np.zeros((h + drip_room, w), bool)
    body[:h] = m
    # escorridos: em algumas colunas da borda de baixo das letras
    bottoms = [x for x in range(w) if m[:, x].any() and m[h - 1, x]]
    for x in rng.sample(bottoms, max(1, len(bottoms) // 5)):
        length = rng.randint(2, drip_room - 2)
        body[h:h + length, x] = True
        if rng.random() < 0.6:
            body[h + length - 1:h + length + 1, max(0, x - 1):x + 1] = True   # gota na ponta
    edge, pad = outline(body)
    W, H = w + 4, h + drip_room + 5
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    # sombra (1 px para baixo e para a direita)
    paint(img, edge | pad, 2, 3, rgba("000000", 150))
    paint(img, edge, 1, 1, INK)
    for yy, xx in zip(*np.nonzero(pad)):
        y0 = yy - 1
        if y0 >= h:
            c = RED_D                                   # sangue escorrendo
        elif y0 > 0 and not pad[yy - 1, xx]:
            c = RED_HI                                  # brilho na borda de cima
        else:
            t = y0 / max(1, h - 1)
            c = RED if t < 0.55 else (RED_D if t < 0.85 else RED_K)
        img.putpixel((1 + xx, 1 + yy), c)
    return img


def small_line(text, size, color):
    m = text_mask(text, size, spacing=0, font_path=SMALL_FONT)
    edge, pad = outline(m)
    img = Image.new("RGBA", (pad.shape[1] + 2, pad.shape[0] + 2), (0, 0, 0, 0))
    paint(img, edge | pad, 2, 2, rgba("000000", 140))
    paint(img, edge, 1, 1, INK)
    paint(img, pad, 1, 1, color)
    # sombra interna nos pés das letras
    for yy, xx in zip(*np.nonzero(pad)):
        if yy + 1 < pad.shape[0] and not pad[yy + 1, xx]:
            img.putpixel((1 + xx, 1 + yy), CREAM_D)
    return img


def flask():
    """Frasco de laboratório (erlenmeyer) com líquido verde e bolhas: o "Ciência"."""
    rows = [
        "...kkkk...",
        "...kggk...",
        "...kggk...",
        "...kggk...",
        "..kggggk..",
        "..kgggggk.",
        ".kgLGGGGk.",
        ".kGGLGGGGk",
        "kGGGGGGLGk",
        "kGGLGGGGGk",
        ".kkkkkkkk.",
    ]
    key = {"k": INK, "g": GLASS, "G": GREEN, "L": GREEN_L}
    img = Image.new("RGBA", (10, 14), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch in key:
                img.putpixel((x, y + 3), key[ch])
    for x, y in ((4, 1), (6, 0), (5, 2)):   # bolhas saindo
        img.putpixel((x, y), GREEN_L)
    return img


def ghost():
    """Primeiro quadro do fantasma comum do jogo (espiando atrás da última letra)."""
    sheet = Image.open(os.path.join(ROOT, "sprites", "characters", "fantasma_normal.png")).convert("RGBA")
    fh = sheet.height if sheet.height <= 24 else 20
    return sheet.crop((0, 0, 16, fh))


def compose(top, word, seed, with_ghost=True):
    head = small_line(top, 8, CREAM)
    big = big_word(word, 32, seed)
    icon = flask()
    gap = 18 if with_ghost else 0
    width = max(head.width + icon.width + 4, big.width + 2 * gap) + 6
    height = head.height + big.height + 2
    img = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    hx = (width - head.width - icon.width - 3) // 2
    img.alpha_composite(icon, (hx, max(0, head.height - icon.height)))
    img.alpha_composite(head, (hx + icon.width + 3, 0))
    bx = (width - big.width) // 2
    if with_ghost:
        g = ghost()
        img.alpha_composite(g, (bx + big.width + 1, head.height + 4))   # fantasma flutuando ao lado do R
    img.alpha_composite(big, (bx, head.height + 1))
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    compose("INSTITUTO DE CIÊNCIA", "E TERROR", 13).save(os.path.join(OUT, "logo.png"))
    compose("INSTITUTO DE CIÊNCIA E TERROR", "PAUSADO", 7, with_ghost=False).save(os.path.join(OUT, "logo_pausado.png"))
    print("logo ok")


if __name__ == "__main__":
    main()
