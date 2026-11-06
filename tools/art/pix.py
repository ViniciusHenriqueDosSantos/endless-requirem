# Ferramentas de pixel art usadas por todos os geradores de sprite.
#
# Regra do projeto: 1 pixel da arte = 4 pixels do jogo (o tile tem 16 px na arte e 64 px no jogo).
# Tudo é desenhado em tamanho real da arte e a Godot amplia com filtro "nearest".
import os, random
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SCALE = 4  # pixels do jogo por pixel da arte


def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def mix(c1, c2, t):
    return tuple(int(round(c1[i] + (c2[i] - c1[i]) * t)) for i in range(4))


def shade(c, k):
    """k < 0 escurece, k > 0 clareia (0..1)."""
    target = (0, 0, 0, c[3]) if k < 0 else (255, 255, 255, c[3])
    return mix(c, target, abs(k))


class Canvas:
    def __init__(self, w, h, fill=(0, 0, 0, 0)):
        self.w, self.h = w, h
        self.img = Image.new("RGBA", (w, h), fill)
        self.px = self.img.load()

    # ---------- básico ----------
    def put(self, x, y, c):
        if c is None: return
        if 0 <= x < self.w and 0 <= y < self.h:
            if len(c) == 4 and c[3] < 255:
                base = self.px[x, y]
                a = c[3] / 255
                out_a = a + base[3] / 255 * (1 - a)
                if out_a <= 0: return
                rgb = [int((c[i] * a + base[i] * (base[3] / 255) * (1 - a)) / out_a) for i in range(3)]
                self.px[x, y] = (*rgb, int(out_a * 255))
            else:
                self.px[x, y] = c

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[x, y]
        return (0, 0, 0, 0)

    def rect(self, x, y, w, h, c):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                self.put(xx, yy, c)

    def frame(self, x, y, w, h, c):
        self.hline(x, y, w, c); self.hline(x, y + h - 1, w, c)
        self.vline(x, y, h, c); self.vline(x + w - 1, y, h, c)

    def hline(self, x, y, w, c):
        for xx in range(x, x + w): self.put(xx, y, c)

    def vline(self, x, y, h, c):
        for yy in range(y, y + h): self.put(x, yy, c)

    def line(self, x0, y0, x1, y1, c):
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            self.put(x0, y0, c)
            if x0 == x1 and y0 == y1: break
            e2 = 2 * err
            if e2 >= dy: err += dy; x0 += sx
            if e2 <= dx: err += dx; y0 += sy

    def ellipse(self, cx, cy, rx, ry, c):
        for yy in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for xx in range(int(cx - rx) - 1, int(cx + rx) + 2):
                if ((xx + 0.5 - cx) / rx) ** 2 + ((yy + 0.5 - cy) / ry) ** 2 <= 1:
                    self.put(xx, yy, c)

    def blit(self, other, x, y, flip=False):
        src = other.img.transpose(Image.FLIP_LEFT_RIGHT) if flip else other.img
        p = src.load()
        for yy in range(src.height):
            for xx in range(src.width):
                c = p[xx, yy]
                if c[3]: self.put(x + xx, y + yy, c)

    def noise(self, x, y, w, h, colors, density, rng):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                if rng.random() < density:
                    self.put(xx, yy, rng.choice(colors))

    def outline(self, c, diagonal=False):
        """Contorno de 1 px em volta de tudo que é opaco (o estilo "sticker" dos personagens)."""
        src = self.img.copy().load()
        nb = [(1, 0), (-1, 0), (0, 1), (0, -1)]
        if diagonal: nb += [(1, 1), (-1, 1), (1, -1), (-1, -1)]
        for y in range(self.h):
            for x in range(self.w):
                if src[x, y][3]: continue
                for dx, dy in nb:
                    xx, yy = x + dx, y + dy
                    if 0 <= xx < self.w and 0 <= yy < self.h and src[xx, yy][3] > 100:
                        self.px[x, y] = c
                        break

    def ascii(self, rows, key, x=0, y=0):
        """Desenha um mapa de caracteres. key: {'a': cor, ...}; '.' ou ' ' = transparente."""
        for j, row in enumerate(rows):
            for i, ch in enumerate(row):
                if ch in ". ": continue
                self.put(x + i, y + j, key[ch])

    def save(self, rel_path):
        path = os.path.join(ROOT, rel_path)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        self.img.save(path)
        return path


def sheet(frames, cols):
    """Junta quadros do mesmo tamanho numa folha (cols por linha)."""
    w, h = frames[0].w, frames[0].h
    rows = (len(frames) + cols - 1) // cols
    out = Canvas(w * cols, h * rows)
    for i, f in enumerate(frames):
        out.blit(f, (i % cols) * w, (i // cols) * h)
    return out


# ---------- fonte de 3x5 para placas e letreiros ----------
FONT = {
    "A": ["010", "101", "111", "101", "101"], "B": ["110", "101", "110", "101", "110"],
    "C": ["011", "100", "100", "100", "011"], "D": ["110", "101", "101", "101", "110"],
    "E": ["111", "100", "110", "100", "111"], "F": ["111", "100", "110", "100", "100"],
    "G": ["011", "100", "101", "101", "011"], "H": ["101", "101", "111", "101", "101"],
    "I": ["111", "010", "010", "010", "111"], "J": ["001", "001", "001", "101", "010"],
    "K": ["101", "101", "110", "101", "101"], "L": ["100", "100", "100", "100", "111"],
    "M": ["101", "111", "111", "101", "101"], "N": ["110", "101", "101", "101", "101"],
    "O": ["010", "101", "101", "101", "010"], "P": ["110", "101", "110", "100", "100"],
    "Q": ["010", "101", "101", "110", "011"], "R": ["110", "101", "110", "101", "101"],
    "S": ["011", "100", "010", "001", "110"], "T": ["111", "010", "010", "010", "010"],
    "U": ["101", "101", "101", "101", "111"], "V": ["101", "101", "101", "101", "010"],
    "W": ["101", "101", "111", "111", "101"], "X": ["101", "101", "010", "101", "101"],
    "Y": ["101", "101", "010", "010", "010"], "Z": ["111", "001", "010", "100", "111"],
    "0": ["111", "101", "101", "101", "111"], "1": ["010", "110", "010", "010", "111"],
    "2": ["110", "001", "010", "100", "111"], "3": ["110", "001", "010", "001", "110"],
    "4": ["101", "101", "111", "001", "001"], "5": ["111", "100", "110", "001", "110"],
    "6": ["011", "100", "111", "101", "111"], "7": ["111", "001", "010", "010", "010"],
    "8": ["111", "101", "111", "101", "111"], "9": ["111", "101", "111", "001", "110"],
    ".": ["000", "000", "000", "000", "010"], "-": ["000", "000", "111", "000", "000"],
    "·": ["000", "000", "010", "000", "000"], "!": ["010", "010", "010", "000", "010"],
    "/": ["001", "001", "010", "100", "100"], " ": ["000", "000", "000", "000", "000"],
    "Ã": ["111", "010", "101", "111", "101"], "Ç": ["011", "100", "100", "011", "010"],
    "É": ["011", "100", "111", "100", "111"], "Ê": ["010", "101", "111", "100", "111"],
}


def text_width(s, spacing=1):
    return max(0, len(s) * (3 + spacing) - spacing)


def text(cv, s, x, y, c, spacing=1):
    for i, ch in enumerate(s.upper()):
        g = FONT.get(ch, FONT[" "])
        for j, row in enumerate(g):
            for k, bit in enumerate(row):
                if bit == "1":
                    cv.put(x + i * (3 + spacing) + k, y + j, c)


def text_center(cv, s, cx, y, c, spacing=1):
    text(cv, s, cx - text_width(s, spacing) // 2, y, c, spacing)


def rng(seed):
    return random.Random(seed)
