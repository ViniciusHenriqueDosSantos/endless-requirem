# Escolhe o tile de cada célula a partir da planta: material do piso, estilo da parede (autotile) e sombras.
# Usado pelos geradores de fase que têm tileset desenhado (tools/art/tiles_*.py gera o atlas e o .json).
import base64, json, os, struct
from zlib import crc32

HERE = os.path.dirname(__file__)

INTERIOR = {"granito", "ceramica", "aco"}


def load_atlas(name):
    with open(os.path.join(HERE, f"tiles_{name}.json"), encoding="utf-8") as f:
        data = json.load(f)
    return data["tiles"], data["decos"]


def h(*k):
    """Número pseudoaleatório fixo para uma célula (a fase sai igual toda vez que roda o gerador)."""
    return crc32(repr(k).encode()) % 1000 / 1000


def tile_data(cells):
    """cells: {(x, y): (atlas_x, atlas_y)} -> PackedByteArray da TileMapLayer (formato da Godot 4)."""
    b = bytearray(struct.pack('<H', 0))
    for (x, y), (ax, ay) in sorted(cells.items(), key=lambda i: (i[0][1], i[0][0])):
        b += struct.pack('<hhHHHH', x, y, 0, ax, ay, 0)
    return base64.b64encode(bytes(b)).decode()


class Tiler:
    def __init__(self, atlas, floor_mat, walls, outer, deco_rules=None):
        """floor_mat: {(x,y): material}; walls: conjunto das paredes; outer: subconjunto da borda do mapa.
        deco_rules(cell, style, below_material) -> nome da decoração ou None."""
        self.names, self.decos = atlas
        self.mat = floor_mat
        self.walls = walls
        self.outer = outer
        self.deco_rules = deco_rules or (lambda c, s, m: None)

    def at(self, name):
        return tuple(self.names[name])

    # ---------- piso
    def floor_tile(self, c, mat):
        x, y = c
        if mat in self.names:  # nome exato de um tile (ex.: sextavado_vaga0)
            return self.at(mat)
        if mat == "bloquete":
            return self.at(f"bloquete{(x % 2) + 2 * (y % 2)}")  # bloco 2x2 da espinha de peixe
        if mat in ("bloquete_tatil", "bloquete_meiofio"):
            return self.at(f"{mat}{x % 2}")
        if mat in ("asfalto_faixa", "asfalto_meiofio", "calcada_meiofio"):
            return self.at(mat)
        if mat == "aco":
            return self.at(f"aco{(x + y) % 2}")
        if mat == "grama":
            return self.at(f"grama{int(h(x, y) * 2)}")
        r = h(mat, x, y)
        v = 0 if r < 0.45 else 1 if r < 0.75 else 3 if r < 0.95 else 2  # variação 2 é a mais chamativa: rara
        return self.at(f"{mat}{v}")

    def floor_cells(self):
        return {c: self.floor_tile(c, m) for c, m in self.mat.items()}

    # ---------- paredes
    def _base_mat(self, m):
        return m.split("_")[0]

    def style_of(self, c):
        x, y = c
        near = [self._base_mat(self.mat[(x + dx, y + dy)]) for dx in (-1, 0, 1) for dy in (-1, 0, 1)
                if (x + dx, y + dy) in self.mat]
        below = self.mat.get((x, y + 1))
        if below is not None:
            ext = self._base_mat(below) not in INTERIOR
            if not ext: return "interno"
            return "externo" if c in self.outer else "fachada"
        if any(m not in INTERIOR for m in near):
            return "externo" if c in self.outer else "fachada"
        return "interno"

    def wall_cells(self, only_outer):
        out = {}
        for c in self.walls:
            if (c in self.outer) != only_outer: continue
            x, y = c
            style = self.style_of(c)
            if (x, y + 1) in self.mat:  # frente: tem chão logo abaixo
                ew = (1 if (x + 1, y) in self.walls else 0) | (2 if (x - 1, y) in self.walls else 0)
                deco = self.deco_rules(c, style, self.mat[(x, y + 1)]) if ew == 3 else None
                name = f"frente_{style}_{deco}" if deco and deco in self.decos.get(style, []) else f"frente_{style}_{ew}"
            else:
                mask = ((1 if (x, y - 1) in self.walls else 0) | (2 if (x + 1, y) in self.walls else 0) |
                        (4 if (x, y + 1) in self.walls else 0) | (8 if (x - 1, y) in self.walls else 0))
                name = f"topo_{style}_{mask}"
            out[c] = self.at(name)
        return out

    # ---------- sombras de contato (camada por cima do piso)
    def shadow_cells(self):
        out = {}
        for (x, y) in self.mat:
            top = (x, y - 1) in self.walls
            left = (x - 1, y) in self.walls
            right = (x + 1, y) in self.walls
            if top or left or right:
                out[(x, y)] = self.at(f"sombra_{int(top)}{int(left)}{int(right)}")
        return out
