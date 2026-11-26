# Itens, armas, projéteis, portas e objetos de jogo (pilha de papel, alvo do chefe, cura).
# Saída: sprites/items/, sprites/weapons/, sprites/fx/, sprites/objects/
from pix import Canvas, sheet, hexc, mix, shade, rng
from palette import C

INK = C["ink"]


# ---------------------------------------------------------------- coletáveis
def cracha():
    cv = Canvas(10, 13)
    cv.line(2, 0, 4, 4, C["lanyard"]); cv.line(7, 0, 5, 4, C["lanyard"])
    cv.rect(1, 4, 8, 9, hexc("f4f4f0")); cv.rect(1, 4, 8, 3, C["uni_blue"]); cv.put(2, 5, hexc("ffffff"))
    cv.rect(2, 8, 3, 3, hexc("c98b62")); cv.hline(6, 8, 2, hexc("8e959d")); cv.hline(6, 10, 2, hexc("8e959d"))
    cv.outline(INK)
    return cv


def marmita():
    """Marmita de pote de plástico (tipo tupperware): tampa azul com as travas e o pote transparente
    deixando ver a comida (arroz em cima, feijão embaixo)."""
    cv = Canvas(16, 13)
    LID, LID_L, LID_D = hexc("3f7fd0"), hexc("7fb2ee"), hexc("27548f")
    POT, POT_D = hexc("dfe8ee", 170), hexc("aebcc6", 200)
    cv.rect(1, 5, 14, 7, POT); cv.vline(1, 5, 7, POT_D); cv.vline(14, 5, 7, POT_D); cv.hline(1, 11, 14, POT_D)
    cv.rect(2, 6, 12, 2, hexc("f3f0e6")); cv.put(4, 6, hexc("ffffff"))      # arroz
    cv.rect(2, 8, 12, 3, hexc("4a2a1c")); cv.put(6, 9, hexc("6b3d28")); cv.put(11, 8, hexc("8e4a2a"))  # feijão e carne
    cv.rect(0, 1, 16, 4, LID); cv.hline(1, 1, 14, LID_L); cv.hline(0, 4, 16, LID_D)                # tampa
    for x in (0, 15): cv.rect(x, 2, 1, 4, LID_D)                                                   # travas
    cv.rect(6, 2, 4, 1, LID_L)
    cv.outline(INK)
    return cv


def escopeta_item():
    """A Escopeta de Café caída no chão (virada para a esquerda)."""
    cv = Canvas(20, 8)
    RED, RED_D, RED_L = hexc("c0392b"), hexc("8a2a20"), hexc("e0604a")
    CHROME, CHROME_D = hexc("c9ced6"), hexc("8a8e95")
    SPOON, SPOON_D, SPOON_L = hexc("c8955a"), hexc("94683a"), hexc("e0b27a")
    cv.rect(1, 2, 3, 3, CHROME); cv.vline(1, 2, 3, CHROME_D)
    cv.rect(4, 2, 10, 3, RED); cv.hline(4, 2, 10, RED_L); cv.hline(4, 4, 10, RED_D)
    cv.vline(9, 2, 3, hexc("efeadd"))
    cv.rect(11, 5, 3, 2, SPOON_D)
    cv.rect(14, 3, 4, 2, SPOON); cv.hline(14, 3, 4, SPOON_L); cv.rect(17, 2, 3, 4, SPOON); cv.vline(19, 3, 2, SPOON_D)
    cv.outline(INK)
    return cv


def cafezinho():
    """Item de cura: o copinho de café da cantina."""
    cv = Canvas(8, 10)
    cv.put(3, 0, hexc("ffffff", 150)); cv.put(4, 1, hexc("ffffff", 150)); cv.put(3, 2, hexc("ffffff", 120))
    cv.rect(1, 3, 6, 6, hexc("f6f4ee")); cv.vline(6, 3, 6, hexc("c9c5bb"))
    cv.hline(1, 3, 6, hexc("6b3b1f")); cv.hline(2, 9, 4, hexc("c9c5bb"))
    cv.rect(2, 5, 4, 2, hexc("2bb673"))
    cv.outline(INK)
    return cv


# ---------------------------------------------------------------- armas na mão do jogador (apontando para a direita)
def laser():
    cv = Canvas(11, 6)
    cv.rect(1, 1, 8, 3, hexc("e8e9ec")); cv.hline(1, 1, 8, hexc("ffffff")); cv.hline(1, 3, 8, hexc("a8acb4"))
    cv.rect(8, 1, 2, 2, hexc("f2c230")); cv.rect(2, 3, 2, 3, hexc("3a3d44")); cv.put(5, 2, C["uni_blue"])
    cv.outline(INK)
    return cv


def escopeta():
    """Escopeta de Café (na mão, apontando para a direita): o cano é a garrafa térmica vermelha da Tia da Cantina,
    com a tampa cromada na boca; a coronha é uma colher de pau."""
    cv = Canvas(16, 7)
    RED, RED_D, RED_L = hexc("c0392b"), hexc("8a2a20"), hexc("e0604a")
    CHROME, CHROME_D = hexc("c9ced6"), hexc("8a8e95")
    SPOON, SPOON_D, SPOON_L = hexc("c8955a"), hexc("94683a"), hexc("e0b27a")
    cv.rect(0, 2, 4, 2, SPOON); cv.hline(0, 2, 4, SPOON_L); cv.rect(0, 1, 2, 4, SPOON); cv.vline(0, 2, 2, SPOON_D)  # colher
    cv.rect(4, 3, 3, 3, SPOON_D)                                         # empunhadura
    cv.rect(4, 1, 9, 3, RED); cv.hline(4, 1, 9, RED_L); cv.hline(4, 3, 9, RED_D)   # garrafa térmica
    cv.vline(8, 1, 3, hexc("efeadd"))                                    # faixa branca
    cv.rect(13, 1, 3, 3, CHROME); cv.vline(15, 1, 3, CHROME_D)           # tampa cromada (a boca)
    cv.outline(INK)
    return cv


def canhao():
    cv = Canvas(15, 8)
    cv.rect(2, 1, 11, 5, hexc("39404a")); cv.hline(2, 1, 11, hexc("5d6672"))
    for x in (5, 8, 11): cv.vline(x, 2, 3, hexc("53e0ff"))
    cv.rect(12, 2, 2, 3, hexc("9ff1ff")); cv.rect(3, 6, 3, 2, hexc("2a2b30"))
    cv.outline(INK)
    return cv


def rifle_silencio():
    """Rifle do Silêncio (o que a Bibliotecária deixa): coronha de madeira de estante, silenciador roxo
    (o cardigã dela), luneta feita dos óculos dela com a correntinha dourada. Aponta para a direita."""
    cv = Canvas(19, 9)
    WOOD, WOOD_D, WOOD_L = hexc("7a5230"), hexc("553820"), hexc("9c6c40")
    METAL, METAL_L = hexc("3a3e46"), hexc("5d6672")
    PURPLE, PURPLE_D, PURPLE_L = hexc("6a5a8a"), hexc("4a3e66"), hexc("8a7aac")
    GOLD = hexc("d9a62e")
    # coronha e empunhadura
    cv.rect(0, 4, 5, 3, WOOD); cv.hline(0, 4, 5, WOOD_L); cv.hline(0, 6, 5, WOOD_D)
    cv.rect(4, 5, 2, 3, WOOD_D)
    # corpo e cano
    cv.rect(5, 4, 6, 2, METAL); cv.hline(5, 4, 6, METAL_L)
    cv.rect(11, 4, 3, 1, METAL)
    # silenciador roxo, grosso, com anéis
    cv.rect(13, 3, 6, 3, PURPLE); cv.hline(13, 3, 6, PURPLE_L); cv.hline(13, 5, 6, PURPLE_D)
    cv.vline(15, 3, 3, PURPLE_D); cv.vline(17, 3, 3, PURPLE_D)
    # luneta: os óculos dela (duas lentes redondas) e a corrente caindo
    cv.rect(6, 1, 5, 1, METAL)
    for x in (5, 9):
        cv.frame(x, 0, 3, 3, hexc("2a2b30")); cv.put(x + 1, 1, hexc("ffe9a0"))
    cv.put(8, 1, hexc("2a2b30"))
    for i, (x, y) in enumerate([(5, 3), (4, 4), (4, 5)]):
        cv.put(x, y, GOLD)
    cv.outline(INK)
    return cv


def marcador():
    """Bala do Rifle do Silêncio: um marcador de página (fita com a ponta em V). Branco: a cor vem da arma."""
    cv = Canvas(9, 3)
    cv.rect(0, 0, 7, 3, hexc("ffffff", 230)); cv.hline(1, 1, 6, hexc("ffffff"))
    cv.put(7, 0, hexc("ffffff", 230)); cv.put(8, 0, hexc("ffffff", 200))
    cv.put(7, 2, hexc("ffffff", 230)); cv.put(8, 2, hexc("ffffff", 200))
    return cv


# ---------------------------------------------------------------- projéteis (brancos: a cor vem da arma)
def bolt():
    cv = Canvas(7, 3)
    cv.hline(0, 1, 7, hexc("ffffff", 160)); cv.rect(2, 0, 5, 3, hexc("ffffff", 200)); cv.hline(3, 1, 4, hexc("ffffff"))
    return cv


def pellet():
    cv = Canvas(3, 3)
    cv.rect(0, 0, 3, 3, hexc("ffffff", 170)); cv.put(1, 1, hexc("ffffff"))
    return cv


def grao_cafe():
    """Bala da Escopeta de Café: um grão de café (oval com o risco no meio). Claro: a cor vem da arma."""
    cv = Canvas(4, 3)
    cv.rect(1, 0, 2, 3, hexc("ffffff")); cv.vline(0, 1, 1, hexc("ffffff", 220)); cv.vline(3, 1, 1, hexc("ffffff", 220))
    cv.put(1, 1, hexc("6a4a30")); cv.put(2, 1, hexc("6a4a30"))
    return cv


def grampeador():
    """Grampeador (a arma do estudante, apontando para a direita): grampeador de escritório preto e vermelho,
    com o braço de metal em cima e a empunhadura de borracha."""
    cv = Canvas(12, 7)
    BLACK, BLACK_L = hexc("2f3138"), hexc("4a4d55")
    RED, RED_D = hexc("c0392b"), hexc("8a2a20")
    METAL, METAL_L = hexc("9aa0a8"), hexc("c9ced6")
    cv.rect(1, 1, 10, 2, RED); cv.hline(1, 1, 10, hexc("e0604a")); cv.put(10, 2, RED_D)   # braço de cima
    cv.rect(2, 3, 10, 1, METAL); cv.hline(2, 3, 9, METAL_L)                              # trilho dos grampos
    cv.rect(1, 4, 11, 1, BLACK); cv.hline(1, 4, 11, BLACK_L)                             # base
    cv.rect(2, 5, 3, 2, BLACK)                                                           # empunhadura
    cv.put(11, 3, hexc("ffffff"))                                                        # o grampo saindo
    cv.outline(INK)
    return cv


def grampo():
    """Bala do Grampeador: um grampo (U de lado). Branco: a cor vem da arma."""
    cv = Canvas(5, 3)
    cv.hline(0, 0, 4, hexc("ffffff")); cv.hline(0, 2, 4, hexc("ffffff")); cv.vline(4, 0, 3, hexc("ffffff"))
    cv.hline(1, 1, 3, hexc("ffffff", 60))
    return cv


def orb():
    cv = Canvas(7, 7)
    cv.ellipse(3.5, 3.5, 3.5, 3.5, hexc("ffffff", 120)); cv.ellipse(3.5, 3.5, 2.3, 2.3, hexc("ffffff", 220))
    cv.ellipse(3.5, 3.5, 1.2, 1.2, hexc("ffffff"))
    return cv


def salgados():
    """Projéteis da Tia: coxinha, pastel, pão de queijo e kibe (folha de 4 quadros de 9x9)."""
    out = []
    c = Canvas(9, 9)  # coxinha (ponta para a direita, que é a direção do voo)
    c.ellipse(3.5, 4.5, 3.5, 3.5, hexc("d98a3a")); c.line(5, 2, 8, 4, hexc("d98a3a")); c.line(5, 7, 8, 5, hexc("d98a3a"))
    c.rect(5, 3, 3, 3, hexc("d98a3a")); c.put(8, 4, hexc("e9a24f")); c.put(2, 3, hexc("f0b868")); c.put(1, 6, hexc("b5682a"))
    out.append(c)
    p = Canvas(9, 9)  # pastel
    p.rect(0, 2, 9, 5, hexc("e7c27a")); p.hline(0, 2, 9, hexc("f3dca0"))
    for x in range(0, 9, 2): p.put(x, 6, hexc("c19a52"))
    out.append(p)
    q = Canvas(9, 9)  # pão de queijo
    q.ellipse(4.5, 4.5, 3.5, 3.5, hexc("f1d38c")); q.put(3, 3, hexc("fff0c2")); q.put(6, 6, hexc("caa25a"))
    out.append(q)
    k = Canvas(9, 9)  # kibe
    k.ellipse(4.5, 4.5, 4, 2.6, hexc("8e4a2a")); k.line(1, 4, 8, 4, hexc("a8603a")); k.put(6, 3, hexc("b97048"))
    out.append(k)
    for cv in out: cv.outline(INK)
    return sheet(out, 4)


# ---------------------------------------------------------------- objetos de jogo
def pilha(stage):
    """Pilha de papéis da entrada. stage 0 = inteira, 1 = baleada, 2 = espalhada (tamanhos 18x14 e 36x14)."""
    if stage == 2:
        cv = Canvas(36, 14)
        import random
        r = random.Random(5)
        cv.ellipse(18, 10, 15, 3, hexc("000000", 40))
        for _ in range(11):
            x, y = r.randrange(2, 30), r.randrange(3, 11)
            w, h = r.choice([(5, 3), (4, 3), (6, 2)])
            cv.rect(x, y, w, h, hexc("ece8da")); cv.hline(x, y + h - 1, w, hexc("b9b3a0"))
            if r.random() < 0.5: cv.hline(x + 1, y + 1, w - 2, hexc("9aa7c0"))
        return cv
    cv = Canvas(18, 14)
    cv.ellipse(9, 12.5, 9, 1.8, hexc("000000", 60))
    sheets = 6 if stage == 0 else 3
    for i in range(sheets):
        y = 12 - (i + 1) * 2
        x = 1 + (i % 2) - (i % 3 == 0)
        cv.rect(x, y, 15, 2, hexc("ece8da")); cv.hline(x, y + 1, 15, hexc("b9b3a0"))
        if i % 2 == 0: cv.put(x + 3 + i, y, hexc("9aa7c0"))
    top = 12 - sheets * 2
    cv.rect(3, top - 1, 9, 1, hexc("f6f3e8"))
    if stage == 0:
        cv.rect(10, top - 3, 6, 2, hexc("e8d27a")); cv.hline(10, top - 2, 6, hexc("b9a24f"))  # post-it / pasta amarela
    else:
        cv.put(13, top, hexc("ece8da")); cv.put(2, top + 1, hexc("ece8da"))
    cv.outline(hexc("5b5648"))
    return cv


def alvo(frame):
    """Alvo que liga o escudo da Tia: plaquinha de 'PROMOÇÃO' com um alvo, em cima de um pedestal."""
    cv = Canvas(16, 22)
    cv.ellipse(8, 20.5, 5, 1.5, hexc("000000", 60))
    cv.rect(7, 10, 2, 10, hexc("4b4f57")); cv.vline(7, 10, 10, hexc("6f747d")); cv.rect(5, 19, 6, 2, hexc("3a3d44"))
    pulse = 0.5 if frame == 1 else 0
    cv.ellipse(8, 7, 6.5 + pulse, 6.5 + pulse, hexc("d62f2f"))
    cv.ellipse(8, 7, 4.5, 4.5, hexc("f6f4ee"))
    cv.ellipse(8, 7, 2.5, 2.5, hexc("d62f2f"))
    cv.put(8, 7, hexc("ffffff") if frame else hexc("f6f4ee"))
    cv.put(4, 3, hexc("ff8a80"))
    cv.outline(INK)
    return cv


def grade(vertical):
    """Grade de aço de enrolar (porta trancada), em tile de 16x16 que se repete pelo tamanho da porta."""
    cv = Canvas(16, 16)
    if vertical:  # vista de cima: trilho e a grade fina
        cv.rect(5, 0, 6, 16, hexc("5e656e"))
        for y in range(0, 16, 2): cv.hline(5, y, 6, hexc("8f98a2"))
        cv.vline(5, 0, 16, hexc("b6bec7")); cv.vline(10, 0, 16, hexc("3e444c"))
    else:
        cv.rect(0, 0, 16, 16, hexc("6d757f", 235))
        for x in range(0, 16, 3):
            cv.vline(x, 0, 16, hexc("aeb6bf")); cv.vline(x + 1, 0, 16, hexc("4b525a"))
        for y in (0, 5, 10, 15): cv.hline(0, y, 16, hexc("3e444c"))
        cv.hline(0, 1, 16, hexc("c9d0d7"))
    return cv


def porta_escada():
    """Porta corta-fogo da escada (16x32), vista de cima numa parede lateral: o batente na espessura da parede,
    a folha de aço pintada, corrente com cadeado e a luz vermelha da escada vazando pela fresta."""
    import json
    from PIL import Image
    tiles = json.load(open("tools/levelgen/tiles_fase1.json"))["tiles"]
    atlas = Image.open("sprites/tiles/fase1_tiles.png").convert("RGBA")
    ax, ay = tiles["topo_interno_5"]
    wall = atlas.crop((ax * 16, ay * 16, ax * 16 + 16, ay * 16 + 16))
    cv = Canvas(16, 32)
    for y0 in (0, 16):
        for y in range(16):
            for x in range(16):
                cv.px[x, y0 + y] = wall.getpixel((x, y))
    cv.rect(2, 3, 12, 26, hexc("3b3f45"))                       # vão do batente
    cv.hline(2, 3, 12, hexc("2a2d32")); cv.hline(2, 28, 12, hexc("7e868f")); cv.hline(2, 2, 12, hexc("7e868f"))
    cv.vline(8, 4, 24, hexc("6e1a17")); cv.vline(9, 6, 20, hexc("4a1412"))  # luz vermelha pela fresta
    LEAF, LEAF_L, LEAF_D = hexc("4f5d58"), hexc("7d8f88"), hexc("323b38")
    cv.rect(3, 4, 5, 24, LEAF); cv.vline(3, 4, 24, LEAF_L); cv.vline(7, 4, 24, LEAF_D)
    for (x, y) in ((5, 7), (4, 11), (6, 20), (5, 24), (6, 9)):
        cv.put(x, y, hexc("7a4a2c"))                            # ferrugem
    for y in range(25, 28):                                     # faixa de aviso amarela e preta
        for x in range(4, 7):
            cv.put(x, y, hexc("d8ad2e") if (x + y) % 2 else hexc("23252a"))
    cv.vline(1, 13, 6, hexc("c9cfd4")); cv.put(2, 13, hexc("8f969d")); cv.put(2, 18, hexc("8f969d"))  # barra antipânico
    for i in range(12):                                          # corrente em X
        for x, y in ((1 + i * 7 // 11, 9 + i), (8 - i * 7 // 11, 9 + i)):
            cv.put(x, y, hexc("a3a9af") if i % 2 else hexc("5d636a"))
    cv.rect(3, 15, 3, 3, hexc("d9a83a")); cv.hline(3, 17, 3, hexc("9c7420")); cv.put(4, 14, hexc("8f969d"))  # cadeado
    return cv


def catraca(locked):
    """Catraca da portaria (32x16): braços de aço e a luzinha vermelha/verde."""
    cv = Canvas(32, 18)
    cv.ellipse(16, 16.5, 14, 1.5, hexc("000000", 60))
    for x0 in (1, 23):
        cv.rect(x0, 3, 8, 13, hexc("b9c0c7")); cv.hline(x0, 3, 8, hexc("e1e6ea")); cv.vline(x0 + 7, 3, 13, hexc("7e868f"))
        cv.rect(x0 + 2, 5, 4, 2, hexc("2a2d33"))
        cv.rect(x0 + 2, 5, 2, 2, hexc("ff3b30") if locked else hexc("38e07a"))
    if locked:  # braços atravessados
        for i, y in enumerate((8, 10, 12)):
            cv.hline(9, y, 14, hexc("d9dee3")); cv.hline(9, y + 1, 14, hexc("8a929b"))
    else:
        cv.vline(10, 9, 6, hexc("d9dee3")); cv.vline(21, 9, 6, hexc("d9dee3"))
    cv.outline(INK)
    return cv


# ---------------------------------------------------------------- inspeção (arte grande, estilo Resident Evil)
GREEN, GREEN_D, GREEN_L = hexc("1f5f36"), hexc("143f24"), hexc("2f8a50")


def cracha_grande():
    """O crachá da UNIFESP SJC visto de perto: cordão verde escrito UNIFESP, presilha preta, cartão branco com o logo,
    GRADUAÇÃO / CIÊNCIA E TECNOLOGIA e a faixa verde SÃO JOSÉ DOS CAMPOS. A foto foi apagada pelo tempo e o plástico
    está riscado (o dono nunca voltou para buscar)."""
    from pix import text, text_center
    W, H = 84, 168
    cv = Canvas(W, H)
    cx = W // 2
    # cordão (laço que sobe e sai pelo topo), com UNIFESP escrito em branco, na vertical
    for side, x0 in ((0, cx - 20), (1, cx + 14)):
        for y in range(0, 44):
            t = y / 44
            x = int(x0 + ((cx - 5 if side == 0 else cx - 1) - x0) * t)
            cv.rect(x, y, 6, 1, GREEN); cv.put(x, y, GREEN_D); cv.put(x + 5, y, GREEN_L)
            # "UNIFESP" em branco ao longo do cordão (traços das letras, na vertical)
            k = y % 16
            if k in (1, 2, 4, 6, 7, 9, 11, 12): cv.put(x + 2, y, hexc("e9f2ea"))
            if k in (1, 3, 4, 6, 8, 9, 11, 13): cv.put(x + 3, y, hexc("e9f2ea"))
    # presilha preta (mosquetão) e argola
    cv.rect(cx - 3, 42, 6, 10, hexc("18181b")); cv.frame(cx - 3, 42, 6, 10, hexc("2e2e33")); cv.put(cx - 1, 44, hexc("55555c"))
    cv.ellipse(cx, 55, 3, 2.5, hexc("1e1e22")); cv.ellipse(cx, 55, 1.5, 1.2, (0, 0, 0, 0))
    # porta-crachá de plástico transparente
    y0 = 58
    cv.rect(4, y0, W - 8, H - y0 - 2, hexc("cfd6db", 170)); cv.frame(4, y0, W - 8, H - y0 - 2, hexc("aab3ba"))
    cv.rect(cx - 8, y0 + 1, 16, 3, hexc("8a939a"))  # fenda
    # cartão
    x0, y1, cw, ch = 8, y0 + 6, W - 16, H - y0 - 12
    cv.rect(x0, y1, cw, ch, hexc("f4f4ef")); cv.frame(x0, y1, cw, ch, hexc("c9c9c2"))
    text_center(cv, "UNIFESP", cx, y1 + 3, GREEN)
    logo = ["     11     ", "   111111   ", " 1111111111 ", "111111111111", " 1 1 11 1 1 ", " 1 1 11 1 1 ", "111111111111"]
    for j, row in enumerate(logo):
        for k, b in enumerate(row):
            if b == "1": cv.rect(cx - 12 + k * 2, y1 + 9 + j * 2, 2, 2, GREEN if j > 2 else GREEN_L)
    cv.hline(x0 + 6, y1 + 23, cw - 12, hexc("9ab3a2"))
    # foto apagada: só sobrou um vulto
    px0, py0 = cx - 11, y1 + 27
    cv.rect(px0, py0, 22, 26, hexc("a9adb0")); cv.noise(px0, py0, 22, 26, [hexc("b8bbbd"), hexc("979b9e")], 0.4, rng(9))
    cv.ellipse(cx, py0 + 10, 5, 6, hexc("7f8488")); cv.rect(px0 + 4, py0 + 18, 14, 8, hexc("7f8488"))
    cv.rect(cx - 4, py0 + 9, 8, 2, hexc("3a3d40"))  # tarja sobre os olhos (desgaste)
    cv.frame(px0, py0, 22, 26, hexc("6b6f73"))
    text_center(cv, "L. MENDES", cx, py0 + 29, hexc("1b1b1e"))
    # o nome foi riscado e raspado com o tempo (quase não dá para ler)
    rr = rng(17)
    for k in range(3):
        cv.line(cx - 19, py0 + 30 + k, cx + 19, py0 + 29 + k + rr.choice((0, 1)), hexc("2a2a2e"))
    for _ in range(14):
        cv.put(rr.randrange(cx - 18, cx + 18), rr.randrange(py0 + 28, py0 + 35), hexc("f4f4ef"))
    text_center(cv, "GRADUACAO", cx, py0 + 37, hexc("3a3a3e"))
    text(cv, "CIENCIA E TECNOLOGIA", x0 + 3, py0 + 43, hexc("3a3a3e"), spacing=0)
    cv.put(x0 + 3 + 3 * 6 + 1, py0 + 42, hexc("3a3a3e"))  # acento
    cv.rect(x0, y1 + ch - 9, cw, 9, GREEN)
    text(cv, "SAO JOSE DOS CAMPOS", x0 + 4, y1 + ch - 7, hexc("eef7f0"), spacing=0)
    # tempo: arranhões no plástico, mancha de umidade, validade
    r = rng(4)
    for _ in range(9):
        xx, yy = r.randrange(6, W - 8), r.randrange(y0 + 2, H - 6)
        cv.line(xx, yy, xx + r.randrange(3, 9), yy + r.randrange(-2, 3), hexc("ffffff", 110))
    cv.ellipse(x0 + cw - 12, y1 + 62, 7, 5, hexc("8a7a4a", 60))
    text(cv, "12/2041", x0 + cw - 27, py0 + 3, hexc("9a3a3a"), spacing=0)
    return cv


def marmita_grande():
    """Marmita de pote de plástico (tipo tupperware), de perto: tampa azul com as quatro travas, pote transparente
    riscado e meio amarelado pelo tempo, a comida aparecendo pelos lados (arroz, feijão, bife, salada). Sem nome."""
    cv = Canvas(72, 52)
    LID, LID_L, LID_D, LID_K = hexc("3f7fd0"), hexc("7fb2ee"), hexc("27548f"), hexc("1b3d68")
    POT, POT_E = hexc("e6ecef", 55), hexc("b9c6cf", 210)
    cv.ellipse(36, 49, 32, 3, hexc("000000", 70))
    # pote transparente: comida em camadas aparecendo pela parede da frente
    cv.rect(4, 20, 64, 27, hexc("f3f0e6"))                                                     # arroz
    for (x, y) in [(8, 22), (15, 24), (22, 21), (30, 23), (41, 22), (50, 24), (58, 21), (63, 23)]: cv.put(x, y, hexc("d9d3c0"))
    cv.rect(4, 30, 64, 9, hexc("4a2a1c"))                                                      # feijão
    for x in range(6, 66, 4): cv.put(x, 32 + (x % 3), hexc("6b3d28"))
    cv.rect(38, 28, 16, 6, hexc("8e4a2a")); cv.hline(38, 28, 16, hexc("b06538"))               # bife
    cv.rect(56, 25, 9, 6, hexc("4fa33a")); cv.put(58, 26, hexc("7fd05a")); cv.put(62, 28, hexc("d7372f"))  # salada
    cv.rect(4, 39, 64, 8, hexc("e0b453", 200))                                                 # farofa no fundo
    cv.rect(2, 18, 68, 30, POT)                                                                # plástico por cima
    cv.vline(2, 18, 30, POT_E); cv.vline(69, 18, 30, POT_E); cv.hline(2, 47, 68, POT_E)
    cv.vline(5, 20, 26, hexc("ffffff", 120)); cv.vline(6, 20, 26, hexc("ffffff", 70))         # brilho do plástico
    for (x0, y0, x1, y1) in [(12, 26, 20, 31), (44, 40, 52, 37), (60, 20, 64, 26)]:          # riscos
        cv.line(x0, y0, x1, y1, hexc("ffffff", 110))
    cv.rect(2, 18, 68, 30, hexc("d9c27a", 25))                                                 # amarelado pelo tempo
    # tampa azul (vista um pouco de cima) com as quatro travas
    cv.rect(0, 6, 72, 13, LID); cv.hline(1, 6, 70, LID_L); cv.hline(0, 18, 72, LID_K); cv.hline(0, 17, 72, LID_D)
    cv.rect(8, 9, 56, 6, hexc("4a8ad8")); cv.frame(8, 9, 56, 6, LID_D); cv.hline(9, 9, 54, LID_L)  # relevo do meio
    for x in (10, 56): cv.rect(x, 2, 6, 5, LID); cv.hline(x, 2, 6, LID_L); cv.rect(x + 1, 3, 4, 1, LID_D)  # travas de trás
    for x in (10, 56): cv.rect(x, 18, 6, 6, LID_D); cv.hline(x, 18, 6, LID); cv.put(x + 2, 22, LID_K)        # travas da frente
    cv.ellipse(62, 11, 3, 1.5, hexc("2b4f7f", 120))                                            # mancha na tampa
    cv.outline(INK)
    return cv


def escopeta_grande():
    """Escopeta de Café, grande (tela de inspeção): garrafa térmica vermelha com o adesivo da cantina,
    tampa cromada, colher de pau de coronha e um fio de vapor saindo da boca."""
    cv = Canvas(112, 34)
    RED, RED_D, RED_L = hexc("c0392b"), hexc("8a2a20"), hexc("e0604a")
    CHROME, CHROME_D = hexc("c9ced6"), hexc("8a8e95")
    SPOON, SPOON_D, SPOON_L = hexc("c8955a"), hexc("94683a"), hexc("e0b27a")
    cv.ellipse(56, 31, 50, 2.5, hexc("000000", 60))
    # colher de pau (coronha): cabo comprido e a concha
    cv.rect(10, 13, 26, 5, SPOON); cv.hline(10, 13, 26, SPOON_L); cv.hline(10, 17, 26, SPOON_D)
    cv.ellipse(8, 15.5, 7, 6, SPOON); cv.ellipse(7, 14.5, 4, 3.5, SPOON_D)
    cv.rect(36, 17, 8, 10, SPOON_D); cv.hline(36, 17, 8, SPOON)          # empunhadura
    cv.rect(44, 18, 6, 5, hexc("2a2b30")); cv.put(46, 21, hexc("8a8e95"))  # gatilho
    # garrafa térmica (o cano)
    cv.rect(38, 7, 56, 12, RED); cv.hline(38, 7, 56, RED_L); cv.hline(38, 8, 56, RED_L); cv.hline(38, 18, 56, RED_D)
    cv.rect(58, 9, 16, 8, hexc("efeadd"))                                # adesivo
    cv.rect(60, 11, 12, 1, hexc("8a2a20")); cv.rect(60, 13, 9, 1, hexc("8a2a20"))
    cv.rect(94, 6, 12, 14, CHROME); cv.vline(105, 6, 14, CHROME_D); cv.hline(94, 6, 12, hexc("ffffff"))   # tampa
    cv.rect(106, 10, 3, 6, hexc("2a2b30"))                               # boca
    for i, (x, y) in enumerate([(108, 7), (110, 4), (108, 1)]):         # vapor
        cv.put(x, y, hexc("ffffff", 150 - i * 30))
    cv.outline(INK)
    return cv


if __name__ == "__main__":
    for name, fn in {"cracha": cracha, "marmita": marmita, "escopeta": escopeta_item, "cafezinho": cafezinho}.items():
        fn().save(f"sprites/items/{name}.png".replace("sprites", "sprites", 1))
    for name, fn in {"laser": laser, "escopeta": escopeta, "canhao": canhao}.items():
        fn().save(f"sprites/weapons/{name}.png")
    rifle_silencio().save("sprites/weapons/rifle_silencio.png")
    grampeador().save("sprites/weapons/grampeador.png")
    grampo().save("sprites/fx/grampo.png")
    grao_cafe().save("sprites/fx/grao_cafe.png")
    marcador().save("sprites/fx/marcador.png")
    for name, fn in {"bolt": bolt, "pellet": pellet, "orb": orb}.items():
        fn().save(f"sprites/fx/{name}.png")
    salgados().save("sprites/fx/salgados.png")
    for i in range(3): pilha(i).save(f"sprites/objects/pilha_{i}.png")
    sheet([alvo(0), alvo(1)], 2).save("sprites/objects/alvo.png")
    grade(False).save("sprites/objects/grade_h.png"); grade(True).save("sprites/objects/grade_v.png")
    porta_escada().save("sprites/objects/porta_escada.png")
    catraca(True).save("sprites/objects/catraca_trancada.png"); catraca(False).save("sprites/objects/catraca_aberta.png")
    cracha_grande().save("sprites/items/inspecao/cracha.png")
    marmita_grande().save("sprites/items/inspecao/marmita.png")
    escopeta_grande().save("sprites/items/inspecao/escopeta.png")
    print("itens ok")
