# Confere se as paredes de uma cena seguram o jogador (ninguém atravessa parede).
#
#   python3 tools/levelgen/check_walls.py levels/fase_1.tscn [levels/fase_2.tscn ...]
#
# Para cada cena, olha as camadas de tiles "Floor", "Walls" e "OuterWalls" e o tileset de cada uma:
#   1. todo tile colocado em Walls/OuterWalls precisa ter colisão no tileset;
#   2. todo tile de chão só pode encostar (inclusive na diagonal) em chão ou em parede com colisão.
#      Se encosta em vazio ou em parede sem colisão, o jogador sai do mapa por ali.
# Os geradores (gen_fase1.py, gen_fase2.py, gen_escada.py) chamam isto no fim e param com erro se algo falhar.
# Termina com código 1 se encontrar problema (dá para usar antes de um commit).
import base64, os, re, struct, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def _res_path(res):
    return os.path.join(ROOT, res.replace("res://", ""))


def _tiles(block):
    m = re.search(r'tile_map_data = PackedByteArray\("([^"]*)"\)', block)
    if not m:
        return {}
    data = base64.b64decode(m.group(1))
    cells = {}
    for i in range(2, len(data), 12):
        x, y, _src, ax, ay, _alt = struct.unpack("<hhHHHH", data[i:i + 12])
        cells[(x, y)] = (ax, ay)
    return cells


def _solid_tiles(tileset_path):
    text = open(tileset_path, encoding="utf-8").read()
    return {(int(a), int(b)) for a, b in re.findall(r"^(\d+):(\d+)/0/physics_layer_0/polygon_\d+/points", text, re.M)}


def check(scene_path):
    """Devolve uma lista de problemas (vazia = tudo certo)."""
    text = open(scene_path, encoding="utf-8").read()
    ext = dict((i, p) for p, i in re.findall(r'\[ext_resource type="TileSet"[^\]]*path="([^"]+)"[^\]]*id="([^"]+)"\]', text))
    layers = {}
    for block in text.split("\n\n"):
        m = re.match(r'\[node name="(Floor|Walls|OuterWalls)"', block)
        if not m:
            continue
        ts = re.search(r'tile_set = ExtResource\("([^"]+)"\)', block)
        layers[m.group(1)] = (_tiles(block), _solid_tiles(_res_path(ext[ts.group(1)])) if ts else set())

    problems = []
    floor = layers.get("Floor", ({}, set()))[0]
    solid = set()
    for name in ("Walls", "OuterWalls"):
        cells, solid_tiles = layers.get(name, ({}, set()))
        bad = sorted(c for c, t in cells.items() if t not in solid_tiles)
        if bad:
            atlas = sorted({cells[c] for c in bad})
            problems.append(f"{name}: {len(bad)} tile(s) de parede SEM colisão (atlas {atlas}), ex.: {bad[:6]}")
        solid |= {c for c, t in cells.items() if t in solid_tiles}

    leaks = sorted({(x + dx, y + dy) for (x, y) in floor for dx in (-1, 0, 1) for dy in (-1, 0, 1)
                    if (x + dx, y + dy) not in floor and (x + dx, y + dy) not in solid})
    if leaks:
        problems.append(f"chão encostado em lugar sem parede sólida em {len(leaks)} tile(s), ex.: {leaks[:10]}")
    return problems


def check_or_fail(scene_path):
    problems = check(scene_path)
    name = os.path.relpath(scene_path, ROOT)
    if problems:
        print(f"PAREDES COM PROBLEMA em {name}:")
        for p in problems:
            print("  - " + p)
        sys.exit(1)
    print(f"paredes ok: {name}")


if __name__ == "__main__":
    failed = False
    for path in sys.argv[1:]:
        problems = check(path)
        if problems:
            failed = True
            print(f"PAREDES COM PROBLEMA em {path}:")
            for p in problems:
                print("  - " + p)
        else:
            print(f"paredes ok: {path}")
    sys.exit(1 if failed else 0)
