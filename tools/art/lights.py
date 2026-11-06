# Texturas de luz (degradês) usadas pelas PointLight2D: luz redonda, lanterna em cone e luz de tubo fluorescente.
# Saída: sprites/lights/
import math
from PIL import Image
from pix import ROOT
import os

OUT = os.path.join(ROOT, "sprites", "lights")
os.makedirs(OUT, exist_ok=True)


def radial(size=256, falloff=1.8):
    im = Image.new("RGBA", (size, size))
    px = im.load(); c = size / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x + 0.5 - c, y + 0.5 - c) / c
            a = max(0.0, 1 - d) ** falloff
            px[x, y] = (255, 255, 255, int(a * 255))
    return im


def cone(w=512, h=512, half_angle=0.42, falloff=1.4):
    """Lanterna: o vértice fica no centro da textura e o facho aponta para a direita."""
    im = Image.new("RGBA", (w, h))
    px = im.load(); cx, cy = w / 2, h / 2
    for y in range(h):
        for x in range(w):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            d = math.hypot(dx, dy) / (w / 2)
            if dx <= 0 or d >= 1: continue
            ang = abs(math.atan2(dy, dx))
            edge = max(0.0, 1 - ang / half_angle)
            if edge <= 0: continue
            a = (1 - d) ** falloff * min(1.0, edge * 2.2) * min(1.0, d * 6 + 0.25)
            px[x, y] = (255, 255, 255, int(min(1, a) * 255))
    return im


def tube(w=256, h=128):
    """Luz alongada (fileira de lâmpadas fluorescentes do teto)."""
    im = Image.new("RGBA", (w, h))
    px = im.load()
    for y in range(h):
        for x in range(w):
            dx = (x + 0.5 - w / 2) / (w / 2); dy = (y + 0.5 - h / 2) / (h / 2)
            d = math.sqrt(dx * dx * 0.7 + dy * dy)
            a = max(0.0, 1 - d) ** 1.6
            px[x, y] = (255, 255, 255, int(a * 255))
    return im


if __name__ == "__main__":
    radial().save(os.path.join(OUT, "radial.png"))
    cone().save(os.path.join(OUT, "cone.png"))
    tube().save(os.path.join(OUT, "tubo.png"))
    print("luzes ok")
