# Personagens: o estudante (jogador), os fantasmas (3 tipos) e a Tia da Cantina.
# Saída: sprites/characters/*.png (folhas de quadros; o tamanho de cada quadro está no nome da função).
from pix import Canvas, sheet, hexc, mix, shade
from palette import C

OUT = "sprites/characters/"
INK = C["ink"]


# ======================================================================== ESTUDANTE (16x24)
# Folha 4 colunas x 3 linhas: linha 0 = de frente (descendo), 1 = de costas (subindo), 2 = de lado (direita).
# Coluna 0 = parado; 0..3 = ciclo de caminhada.
def student(direction, frame):
    cv = Canvas(16, 24)
    bob = 1 if frame in (1, 3) else 0
    step = {0: 0, 1: 1, 2: 0, 3: -1}[frame]
    y0 = bob  # corpo desce 1 px nos passos

    # --- pernas e tênis
    if direction == "side":
        a, b = (step, -step)
        for i, off in enumerate((a, b)):
            x = 7 + off * 2
            col_j = C["jeans"] if i == 0 else C["jeans_d"]
            cv.rect(x, 17 + y0, 3, 4 - y0, col_j)
            cv.rect(x, 21, 4, 2, C["shoe"]); cv.hline(x, 22, 4, C["shoe_d"])
    else:
        for i, x in enumerate((5, 8)):
            lift = (step if i == 0 else -step)
            top = 17 + y0
            h = 4 - y0 - max(lift, 0)
            cv.rect(x, top, 3, max(h, 1), C["jeans"] if i == 0 else C["jeans_d"])
            sy = 21 - max(lift, 0)
            cv.rect(x, sy, 3, 2, C["shoe"]); cv.hline(x, sy + 1, 3, C["shoe_d"])

    # --- tronco (moletom azul-marinho)
    if direction == "side":
        cv.rect(5, 10 + y0, 6, 8, C["hood"]); cv.vline(10, 10 + y0, 8, C["hood_d"]); cv.vline(5, 11 + y0, 6, C["hood_l"])
        cv.rect(2, 10 + y0, 4, 7, C["pack"]); cv.vline(2, 11 + y0, 5, C["pack_d"]); cv.put(3, 12 + y0, shade(C["pack"], 0.2))
        arm = -step
        cv.rect(7 + arm, 11 + y0, 2, 5, C["hood_l"]); cv.rect(7 + arm, 16 + y0, 2, 1, C["skin"])
    else:
        cv.rect(4, 10 + y0, 8, 8, C["hood"])
        cv.vline(4, 10 + y0, 8, C["hood_d"]); cv.vline(11, 10 + y0, 8, C["hood_d"])
        cv.hline(4, 17 + y0, 8, C["hood_d"])
        sw = step
        cv.rect(3, 11 + y0 + max(sw, 0), 1, 5, C["hood_d"]); cv.rect(12, 11 + y0 + max(-sw, 0), 1, 5, C["hood_d"])
        cv.put(3, 16 + y0 + max(sw, 0), C["skin"]); cv.put(12, 16 + y0 + max(-sw, 0), C["skin"])
        if direction == "down":
            cv.hline(5, 10 + y0, 6, C["hood_l"])
            cv.vline(5, 10 + y0, 3, C["pack"]); cv.vline(10, 10 + y0, 3, C["pack"])  # alças da mochila
            cv.line(6, 10 + y0, 7, 12 + y0, C["lanyard"]); cv.line(9, 10 + y0, 8, 12 + y0, C["lanyard"])
            cv.rect(7, 13 + y0, 2, 2, hexc("f4f4f0")); cv.put(7, 13 + y0, C["uni_blue"])  # crachá
            cv.rect(5, 15 + y0, 6, 1, C["hood_l"])  # bolso canguru
        else:  # costas: mochila grande
            cv.rect(4, 10 + y0, 8, 6, C["pack"]); cv.hline(4, 10 + y0, 8, shade(C["pack"], 0.2))
            cv.rect(5, 13 + y0, 6, 3, C["pack_d"]); cv.put(8, 12 + y0, hexc("d9c39a"))

    # --- cabeça
    hy = 1 + y0
    if direction == "side":
        cv.rect(5, hy + 2, 7, 7, C["skin"]); cv.vline(5, hy + 4, 4, C["skin_d"])
        cv.rect(4, hy, 7, 4, C["hair"]); cv.rect(4, hy + 3, 3, 4, C["hair"]); cv.hline(5, hy, 5, C["hair_l"])
        cv.put(10, hy + 5, INK); cv.put(11, hy + 7, C["skin_d"])
        cv.put(6, hy + 6, C["skin_d"])
    else:
        cv.rect(4, hy + 2, 8, 7, C["skin"])
        cv.vline(4, hy + 4, 4, C["skin_d"]); cv.vline(11, hy + 4, 4, C["skin_d"])
        if direction == "down":
            cv.rect(4, hy, 8, 3, C["hair"]); cv.hline(5, hy, 6, C["hair_l"])
            cv.put(4, hy + 3, C["hair"]); cv.put(11, hy + 3, C["hair"]); cv.rect(8, hy + 3, 3, 1, C["hair"])
            cv.put(6, hy + 5, INK); cv.put(9, hy + 5, INK)
            cv.put(6, hy + 4, C["hair"]); cv.put(9, hy + 4, C["hair"])
            cv.put(5, hy + 6, C["skin_l"]); cv.put(10, hy + 6, C["skin_l"])
            cv.hline(7, hy + 7, 2, C["skin_d"])
        else:
            cv.rect(4, hy, 8, 7, C["hair"]); cv.hline(5, hy, 6, C["hair_l"]); cv.hline(5, hy + 6, 6, shade(C["hair"], 0.1))
    cv.outline(INK)
    # sombra no chão, só onde não tem desenho
    for yy in (22, 23):
        for xx in range(3, 13):
            if not cv.get(xx, yy)[3] and abs(xx - 7.5) < (5.5 if yy == 22 else 4):
                cv.put(xx, yy, hexc("000000", 70))
    return cv


def player_sheet():
    frames = [student(d, f) for d in ("down", "up", "side") for f in range(4)]
    return sheet(frames, 4)


# ======================================================================== FANTASMAS
def ghost(w, h, frame, body, body_m, body_d, eye_glow, extras):
    """Fantasma de lençol, flutuando (4 quadros). Origem nos 'pés' (embaixo)."""
    cv = Canvas(w, h)
    bob = [0, -1, -1, 0][frame]
    cx = w / 2
    top = 3 + bob + extras.get("top", 0)
    rx = w / 2 - 1.5
    bottom = h - 3 + bob
    # sombra no chão (não sobe com o fantasma)
    cv.ellipse(cx, h - 1.5, rx * 0.7, 1.5, hexc("000000", 60))
    # corpo
    cv.ellipse(cx, top + rx, rx, rx, body)
    cv.rect(int(cx - rx), int(top + rx), int(rx * 2) + 1, int(bottom - top - rx), body)
    # barra ondulada
    for x in range(int(cx - rx), int(cx + rx) + 1):
        wave = (x + frame) % 4
        if wave in (0, 1): cv.put(x, int(bottom), body)
        if wave == 0: cv.put(x, int(bottom) + 1, body_m)
    # sombreado (luz vem de cima à esquerda)
    for y in range(h):
        for x in range(w):
            p = cv.get(x, y)
            if p[3] and p[:3] == body[:3]:
                if x > cx + rx * 0.35: cv.put(x, y, body_m)
                if x > cx + rx * 0.7: cv.put(x, y, body_d)
    cv.put(int(cx - rx * 0.5), int(top + 2), hexc("ffffff")); cv.put(int(cx - rx * 0.5) + 1, int(top + 2), hexc("ffffff"))
    # olhos ocos com brilho
    ey = int(top + rx + extras.get("eye_dy", 0))
    for ex in (int(cx - rx * 0.45), int(cx + rx * 0.25)):
        cv.rect(ex, ey - 1, 2, 3, hexc("1b1724")); cv.put(ex + 1, ey, eye_glow)
    if extras.get("mouth"):
        cv.rect(int(cx - 1), ey + 3, 2, 1 + (frame % 2), hexc("1b1724"))
    for fn in extras.get("draw", []):
        fn(cv, frame, bob, cx, top)
    cv.outline(shade(body_d, -0.55))
    # deixa o corpo levemente transparente (é um fantasma), mas o contorno fica firme
    px = cv.img.load()
    for y in range(h):
        for x in range(w):
            r_, g, b, a = px[x, y]
            if a and (r_, g, b) in (body[:3], body_m[:3], body_d[:3]):
                px[x, y] = (r_, g, b, 225)
    return cv


def capelo(cv, frame, bob, cx, top):
    """Capelo de formatura que nunca chegou."""
    y = int(top) - 2
    cv.rect(int(cx) - 5, y, 11, 2, C["cap_grad"]); cv.hline(int(cx) - 5, y, 11, hexc("3b3b46"))
    cv.rect(int(cx) - 3, y + 2, 7, 1, C["cap_grad"])
    cv.put(int(cx), y - 1, C["tassel"])
    tx = int(cx) + 5
    cv.vline(tx, y + 1, 3 + (frame % 2), C["tassel"])


def mochila_laranja(cv, frame, bob, cx, top):
    cv.rect(1, int(top) + 6, 3, 5, hexc("e0782a")); cv.vline(1, int(top) + 6, 5, hexc("b5561a"))
    if frame % 2 == 0:  # risquinhos de velocidade
        for y in (int(top) + 5, int(top) + 8):
            cv.put(0, y, hexc("ffffff", 160))


def livros(cv, frame, bob, cx, top):
    """O jubilado carrega a pilha de livros e o TCC que nunca entregou."""
    y = int(top) + 19
    cols = [hexc("c0392b"), hexc("2e86c1"), hexc("f1c40f"), hexc("27ae60")]
    for i, c in enumerate(cols):
        yy = y - i * 2
        cv.rect(6 + (i % 2), yy, 12, 2, c); cv.hline(6 + (i % 2), yy, 12, shade(c, 0.3))
        cv.vline(17 + (i % 2), yy, 2, hexc("f3efe2"))
    cv.rect(4, y - 1, 3, 4, C["ghost_m"]); cv.rect(17, y - 1, 3, 4, C["ghost_m"])  # mãos


def ghost_sheet(kind):
    if kind == "normal":
        body, m, d = hexc("eef4f8"), hexc("c7d4e0"), hexc("93a4b9")
        frames = [ghost(16, 20, f, body, m, d, hexc("ff4b5c"), {"draw": [capelo], "top": 2, "mouth": True}) for f in range(4)]
    elif kind == "fast":
        body, m, d = hexc("fde9d7"), hexc("f1c39c"), hexc("cf905f")
        frames = [ghost(12, 15, f, body, m, d, hexc("ffb21a"), {"draw": [mochila_laranja]}) for f in range(4)]
    else:
        body, m, d = hexc("e4d8f4"), hexc("c0aee0"), hexc("8e74bd")
        frames = [ghost(24, 28, f, body, m, d, hexc("c26bff"), {"draw": [capelo, livros], "top": 2, "eye_dy": -4, "mouth": True}) for f in range(4)]
    return sheet(frames, 4)


# ======================================================================== TIA DA CANTINA (32x40)
# Quadros: 0 e 1 = parada (respirando), 2 = arremessando salgado, 3 = tonta (escudo caiu)
def tia(frame):
    cv = Canvas(32, 40)
    bob = 1 if frame == 1 else 0
    uni, uni_d, uni_l = hexc("d75f7a"), hexc("a8435b"), hexc("ec8aa0")  # uniforme rosa
    apron, apron_d = hexc("f6f4ef"), hexc("c9c5bb")
    skin, skin_d, skin_l = hexc("d79a74"), hexc("b0775a"), hexc("ebb795")
    y = 2 + bob
    # corpo
    cv.ellipse(16, y + 26, 11, 11, uni)
    cv.rect(6, y + 26, 21, 12 - bob, uni)
    cv.ellipse(16, y + 28, 7, 9, apron); cv.rect(10, y + 28, 13, 10, apron)
    cv.vline(22, y + 26, 12, apron_d)
    cv.hline(10, y + 31, 13, apron_d)  # bolso do avental
    cv.rect(12, y + 32, 9, 3, hexc("efece5")); cv.put(14, y + 33, hexc("2f8f46"))
    # braços
    if frame == 2:  # arremesso: braço direito para cima com uma coxinha
        cv.rect(25, y + 12, 4, 10, uni); cv.rect(25, y + 9, 4, 3, skin)
        cv.rect(26, y + 5, 4, 4, hexc("d98a3a")); cv.put(27, y + 4, hexc("d98a3a")); cv.put(27, y + 6, hexc("f0b060"))
        cv.rect(3, y + 22, 4, 9, uni); cv.rect(3, y + 31, 4, 3, skin)
    elif frame == 3:
        cv.rect(3, y + 22, 4, 9, uni); cv.rect(25, y + 22, 4, 9, uni)
        cv.rect(3, y + 31, 4, 3, skin); cv.rect(25, y + 31, 4, 3, skin)
    else:  # segura a bandeja de salgados
        cv.rect(3, y + 22, 4, 7, uni); cv.rect(25, y + 22, 4, 7, uni)
        cv.rect(4, y + 28, 24, 2, hexc("b9bfc6")); cv.hline(4, y + 28, 24, hexc("e2e6ea"))
        for i, c in enumerate([hexc("d98a3a"), hexc("f1d38c"), hexc("c56b2c"), hexc("d98a3a"), hexc("e7c27a")]):
            cv.rect(6 + i * 4, y + 26, 3, 2, c); cv.put(7 + i * 4, y + 25, c)
    # cabeça
    cv.ellipse(16, y + 12, 8, 8, skin)
    cv.vline(23, y + 9, 7, skin_d); cv.put(9, y + 14, skin_l)
    cv.rect(8, y + 4, 16, 5, hexc("4a2e22"))  # cabelo
    cv.ellipse(16, y + 4, 8, 4, apron); cv.hline(9, y + 7, 14, apron_d)  # touca branca
    for x in range(10, 23, 3): cv.put(x, y + 3, apron_d)
    # rosto
    if frame == 3:  # tonta: olhos em X e estrelinhas
        for ex in (12, 19):
            cv.put(ex, y + 11, INK); cv.put(ex + 1, y + 12, INK); cv.put(ex + 1, y + 11, INK); cv.put(ex, y + 12, INK)
        cv.ellipse(16, y + 16, 2, 1.5, hexc("7a2e2e"))
        for (sx, sy) in [(6, y + 1), (25, y + 2), (16, y - 1)]:
            cv.put(sx, sy, hexc("ffe24a")); cv.put(sx - 1, sy, hexc("ffe24a")); cv.put(sx + 1, sy, hexc("ffe24a"))
            cv.put(sx, sy - 1, hexc("ffe24a")); cv.put(sx, sy + 1, hexc("ffe24a"))
    else:
        # óculos e sobrancelhas bravas
        for ex in (11, 18):
            cv.frame(ex, y + 10, 4, 3, hexc("3a2a2a")); cv.put(ex + 1, y + 11, INK); cv.put(ex + 2, y + 11, hexc("ffffff"))
        cv.hline(15, y + 11, 3, hexc("3a2a2a"))
        cv.line(11, y + 8, 14, y + 9, hexc("3a2318")); cv.line(21, y + 8, 18, y + 9, hexc("3a2318"))
        cv.put(10, y + 14, hexc("e98a8a")); cv.put(22, y + 14, hexc("e98a8a"))
        if frame == 2:
            cv.rect(14, y + 15, 4, 2, hexc("7a2e2e")); cv.hline(15, y + 15, 2, hexc("ffffff"))
        else:
            cv.hline(14, y + 16, 4, hexc("7a2e2e")); cv.put(13, y + 15, hexc("7a2e2e")); cv.put(18, y + 15, hexc("7a2e2e"))
    cv.outline(INK)
    return cv


def tia_sheet():
    return sheet([tia(f) for f in range(4)], 4)


if __name__ == "__main__":
    player_sheet().save(OUT + "estudante.png")
    for k in ("normal", "fast", "tank"):
        ghost_sheet(k).save(OUT + f"fantasma_{k}.png")
    tia_sheet().save(OUT + "tia_cantina.png")
    print("personagens ok")
