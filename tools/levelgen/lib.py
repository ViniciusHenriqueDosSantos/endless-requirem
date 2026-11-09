# Funções comuns dos geradores de fase (planta em tiles -> cena .tscn da Godot).
# Tiles de 16 px mostrados 4x = 64 px no jogo. Origem dos objetos na BASE (visão 3/4).
import base64, struct

T = 64


class Level:
    def __init__(self):
        self.floor = set()
        self.nodes = []
        self.ext = []
        self._ext_ids = {}

    # ---------- planta ----------
    def add_floor(self, x0, y0, x1, y1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.floor.add((x, y))

    def build_walls(self):
        """Parede = todo vazio encostado no chão. Parede de fora = encostada no vazio de fora do mapa."""
        floor = self.floor
        walls = set()
        for (x, y) in floor:
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    c = (x + dx, y + dy)
                    if c not in floor:
                        walls.add(c)
        xs = [c[0] for c in walls]; ys = [c[1] for c in walls]
        bx0, bx1, by0, by1 = min(xs) - 1, max(xs) + 1, min(ys) - 1, max(ys) + 1
        outside, stack = set(), [(bx0, by0)]
        while stack:
            c = stack.pop()
            if c in outside or c in floor or c in walls: continue
            if not (bx0 <= c[0] <= bx1 and by0 <= c[1] <= by1): continue
            outside.add(c)
            stack += [(c[0] + 1, c[1]), (c[0] - 1, c[1]), (c[0], c[1] + 1), (c[0], c[1] - 1)]

        def is_outer(c):
            return any((c[0] + dx, c[1] + dy) in outside for dx in (-1, 0, 1) for dy in (-1, 0, 1))

        def tile(c):
            return 3 if (c[0], c[1] + 1) in floor else 2

        self.inner_walls = {c: tile(c) for c in walls if not is_outer(c)}
        self.outer_walls = {c: tile(c) for c in walls if is_outer(c)}
        self.floor_tiles = {c: (c[0] + c[1]) % 2 for c in floor}

    @staticmethod
    def tile_data(cells):
        b = bytearray(struct.pack('<H', 0))
        for (x, y), atlas in sorted(cells.items(), key=lambda i: (i[0][1], i[0][0])):
            b += struct.pack('<hhHHHH', x, y, 0, atlas, 0, 0)
        return base64.b64encode(bytes(b)).decode()

    # ---------- recursos ----------
    def res(self, kind, path, rid):
        if rid not in self._ext_ids:
            self._ext_ids[rid] = path
            self.ext.append((kind, path, rid))
        return f'ExtResource("{rid}")'

    # ---------- nós ----------
    def node(self, name, parent=".", type=None, inst=None, props=None, groups=None, node_paths=None, root=False):
        head = f'[node name="{name}"'
        if type: head += f' type="{type}"'
        if not root: head += f' parent="{parent}"'
        if node_paths: head += ' node_paths=PackedStringArray(' + ', '.join(f'"{p}"' for p in node_paths) + ')'
        if groups: head += ' groups=[' + ', '.join(f'"{g}"' for g in groups) + ']'
        if inst: head += f' instance=ExtResource("{inst}")'
        head += ']'
        lines = [head] + [f"{k} = {v}" for k, v in (props or {}).items()]
        self.nodes.append("\n".join(lines))

    def tile_layers(self):
        self.res('TileSet', 'res://tilesets/placeholder_tileset.tres', 'tiles')
        self.res('TileSet', 'res://tilesets/placeholder_tileset_outer.tres', 'tiles_outer')
        self.node("Floor", type="TileMapLayer", groups=["floor"], props={
            "z_index": "-10", "texture_filter": "1", "scale": v(4, 4),
            "tile_map_data": f'PackedByteArray("{self.tile_data(self.floor_tiles)}")', "tile_set": 'ExtResource("tiles")'})
        self.node("Walls", type="TileMapLayer", groups=["obstacles"], props={
            "y_sort_enabled": "true", "texture_filter": "1", "scale": v(4, 4),
            "tile_map_data": f'PackedByteArray("{self.tile_data(self.inner_walls)}")', "tile_set": 'ExtResource("tiles")'})
        self.node("OuterWalls", type="TileMapLayer", groups=["obstacles", "outer_walls"], props={
            "y_sort_enabled": "true", "texture_filter": "1", "scale": v(4, 4),
            "tile_map_data": f'PackedByteArray("{self.tile_data(self.outer_walls)}")', "tile_set": 'ExtResource("tiles_outer")'})

    def tex(self, name, folder):
        """Textura de sprites/<folder>/<name>.png como ExtResource."""
        path = f"res://sprites/{folder}/{name}.png"
        return self.res('Texture2D', path, "tex_" + f"{folder}_{name}".replace("/", "_"))

    def tiled_layers(self, atlas_name, floor_mat, deco_rule=None, style_rule=None, decal_rules=None, background=None):
        """Camadas com a arte do tileset (tools/art/tiles_<atlas_name>.py): piso por material, sombras de contato,
        detalhes de abandono, paredes com autotile e decoração, e o fundo (o que fica fora das salas).
        decal_rules: lista de (função(material) -> bool, [(tipo, variações, chance), ...]).
        background: função (x, y) -> nome do tile, para as células vazias em volta (None = nada)."""
        from tiling import Tiler, load_atlas, tile_data, h
        walls = set(self.inner_walls) | set(self.outer_walls)
        tiler = Tiler(load_atlas(atlas_name), floor_mat, walls, set(self.outer_walls), deco_rule, style_rule)
        self.tiler = tiler
        self.res('TileSet', f'res://tilesets/{atlas_name}_tileset.tres', 'tiles')
        self.res('TileSet', f'res://tilesets/{atlas_name}_tileset_outer.tres', 'tiles_outer')
        layer = lambda name, z, cells, extra=None, groups=None: self.node(name, type="TileMapLayer", groups=groups, props={
            **({"z_index": str(z)} if z is not None else {}), **(extra or {}), "texture_filter": "1", "scale": v(4, 4),
            "tile_map_data": f'PackedByteArray("{tile_data(cells)}")',
            "tile_set": 'ExtResource("tiles_outer")' if name == "OuterWalls" else 'ExtResource("tiles")'})
        layer("Floor", -10, tiler.floor_cells(), groups=["floor"])
        if background:
            xs = [c[0] for c in walls]; ys = [c[1] for c in walls]
            bg = {}
            for y in range(min(ys) - 8, max(ys) + 9):
                for x in range(min(xs) - 8, max(xs) + 9):
                    if (x, y) in floor_mat or (x, y) in walls: continue
                    n = background(x, y)
                    if n: bg[(x, y)] = tiler.at(n)
            layer("Fundo", -11, bg)
        layer("Sombras", -9, tiler.shadow_cells())
        decals = {}
        self.decal_cells = {}
        for (x, y), m in floor_mat.items():
            for test, rules in (decal_rules or []):
                if not test(m): continue
                roll, acc = h("decal", x, y), 0.0
                for kind, n, p_ in rules:
                    acc += p_
                    if roll < acc:
                        decals[(x, y)] = tiler.at(f"decal_{kind}{int(h('dv', x, y) * n)}")
                        self.decal_cells[(x, y)] = kind
                        break
                break
        layer("Abandono", -8, decals)
        self.wall_tiles = tiler.wall_cells(only_outer=False)
        self.outer_wall_tiles = tiler.wall_cells(only_outer=True)
        layer("Walls", None, self.wall_tiles, {"y_sort_enabled": "true"}, groups=["obstacles"])
        layer("OuterWalls", None, self.outer_wall_tiles, {"y_sort_enabled": "true"}, groups=["obstacles", "outer_walls"])
        return tiler

    def prop(self, name, parent, x, y, w, h, color, text="", depth=None, font=16, text_color=None, solid=True, z=None,
             tall=False, blocks_ghosts=False, inner=None, sprite=None, folder="props/fase2", fade=False):
        self.res('PackedScene', 'res://objects/prop/prop.tscn', 'prop')
        p = {"position": v(x, y), "Size": v(w, h), "Color": color}
        if sprite: p["Texture"] = self.tex(sprite, folder)
        if fade: p["FadeWhenBehind"] = "true"
        if tall: p["Tall"] = "true"
        if blocks_ghosts: p["BlocksGhosts"] = "true"
        if inner: p["InnerColor"] = inner
        if depth is not None: p["Depth"] = f"{depth:g}"
        if text:
            p["Text"] = '"' + text.replace('"', '\\"') + '"'
            p["FontSize"] = str(font)
        if text_color: p["TextColor"] = text_color
        if not solid: p["Solid"] = "false"
        if z is not None: p = {"z_index": str(z), **p}
        self.node(name, parent, inst="prop", props=p)

    def write(self, path, header_node):
        with open(path, "w") as f:
            f.write("[gd_scene format=4]\n\n")
            for kind, p, rid in self.ext:
                f.write(f'[ext_resource type="{kind}" path="{p}" id="{rid}"]\n')
            f.write("\n" + header_node + "\n\n" + "\n\n".join(self.nodes) + "\n")
        from check_walls import check_or_fail
        check_or_fail(path)  # nenhuma parede atravessável


def v(x, y):
    return f"Vector2({x:g}, {y:g})"


def col(r, g, b, a=1):
    return f"Color({r:g}, {g:g}, {b:g}, {a:g})"


def px(tx, ty):
    """Centro do tile em pixels."""
    return (tx * T + T // 2, ty * T + T // 2)


def root_node(name, script_id, paths):
    """Nó raiz com o roteiro da fase e as referências (NodePath) para as peças."""
    head = (f'[node name="{name}" type="Node2D" node_paths=PackedStringArray(' +
            ', '.join(f'"{k}"' for k in paths) + ')]')
    props = {"y_sort_enabled": "true", "script": f'ExtResource("{script_id}")'}
    props.update({k: f'NodePath("{p}")' for k, p in paths.items()})
    return head + "\n" + "\n".join(f"{k} = {val}" for k, val in props.items())


CONCRETE = col(0.62, 0.62, 0.6)
WHITE = col(0.78, 0.79, 0.8)
BEGE = col(0.66, 0.6, 0.5)
WOOD = col(0.48, 0.34, 0.22)
