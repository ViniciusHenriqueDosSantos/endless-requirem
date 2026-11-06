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
    """Marmitex de alumínio aberta, vista de cima em 3/4: borda dobrada, arroz, feijão, bife e salada
    (o formato que todo mundo reconhece), com a tampa de papel-alumínio meio levantada atrás."""
    cv = Canvas(16, 13)
    AL, AL_L, AL_D = hexc("c9d0d6"), hexc("eef2f5"), hexc("8d969f")
    cv.rect(2, 0, 12, 3, AL_L); cv.hline(2, 0, 12, hexc("ffffff")); cv.hline(3, 2, 10, AL)     # tampa levantada
    cv.rect(0, 3, 16, 9, AL); cv.hline(0, 3, 16, AL_L); cv.hline(0, 11, 16, AL_D)              # bandeja
    for x in range(0, 16, 2): cv.put(x, 3, AL_D)                                                # borda dobrada
    cv.rect(2, 5, 6, 5, hexc("f3f0e6")); cv.put(3, 6, hexc("ffffff")); cv.put(6, 8, hexc("ddd8c8"))  # arroz
    cv.rect(8, 5, 6, 3, hexc("4a2a1c")); cv.put(9, 5, hexc("6b3d28")); cv.put(12, 6, hexc("6b3d28"))  # feijão
    cv.rect(8, 8, 4, 2, hexc("8e4a2a")); cv.hline(8, 8, 4, hexc("b06538"))                     # bife
    cv.rect(12, 8, 2, 2, hexc("4fa33a")); cv.put(13, 8, hexc("7fd05a"))                        # salada
    cv.vline(1, 4, 7, AL_D); cv.vline(14, 4, 7, AL_D)
    cv.outline(INK)
    return cv


def escopeta_item():
    cv = Canvas(20, 8)
    cv.rect(1, 2, 13, 2, hexc("4a4d55")); cv.hline(1, 2, 13, hexc("7b808a"))
    cv.rect(13, 2, 6, 4, C["wood"]); cv.hline(13, 2, 6, C["wood_l"]); cv.put(18, 5, C["wood_d"])
    cv.rect(6, 4, 5, 2, C["wood_d"]); cv.put(12, 4, hexc("2a2b30")); cv.put(12, 5, hexc("2a2b30"))
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
    cv = Canvas(15, 6)
    cv.rect(4, 1, 10, 2, hexc("4a4d55")); cv.hline(4, 1, 10, hexc("7b808a"))
    cv.rect(0, 1, 5, 3, C["wood"]); cv.hline(0, 1, 5, C["wood_l"]); cv.rect(5, 3, 4, 2, C["wood_d"])
    cv.outline(INK)
    return cv


def canhao():
    cv = Canvas(15, 8)
    cv.rect(2, 1, 11, 5, hexc("39404a")); cv.hline(2, 1, 11, hexc("5d6672"))
    for x in (5, 8, 11): cv.vline(x, 2, 3, hexc("53e0ff"))
    cv.rect(12, 2, 2, 3, hexc("9ff1ff")); cv.rect(3, 6, 3, 2, hexc("2a2b30"))
    cv.outline(INK)
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
    """Marmitex de alumínio aberta, de perto: borda dobrada, tampa de papel-alumínio levantada com o nome escrito
    à caneta, e a comida de RU (arroz, feijão, bife, farofa, salada). Esquecida há anos: fria, com uma mosca."""
    from pix import text
    cv = Canvas(72, 52)
    AL, AL_L, AL_D, AL_K = hexc("c9d0d6"), hexc("eef2f5"), hexc("8d969f"), hexc("6a737c")
    cv.ellipse(36, 48, 33, 3, hexc("000000", 70))
    # tampa levantada atrás (papelão por dentro, alumínio por fora) com o nome à caneta
    cv.rect(8, 0, 56, 14, hexc("e9e4d6")); cv.frame(8, 0, 56, 14, hexc("b9b2a0")); cv.hline(8, 13, 56, AL_D)
    text(cv, "JOAO", 12, 3, hexc("2b3a8a")); text(cv, "S/ CEBOLA", 30, 3, hexc("2b3a8a"), spacing=0)
    cv.line(12, 10, 36, 10, hexc("2b3a8a"))
    # bandeja
    cv.rect(2, 14, 68, 32, AL); cv.hline(2, 14, 68, AL_L); cv.hline(2, 45, 68, AL_K)
    for x in range(2, 70, 3): cv.put(x, 14, AL_D); cv.put(x + 1, 15, AL_L)                      # borda dobrada
    cv.rect(5, 17, 62, 26, hexc("b3bbc3")); cv.vline(5, 17, 26, AL_D); cv.vline(66, 17, 26, AL_L)
    # comida
    cv.ellipse(18, 29, 12, 10, hexc("f3f0e6"))                                                   # arroz
    for (x, y) in [(12, 25), (16, 23), (22, 27), (14, 32), (20, 34), (24, 30), (10, 29)]: cv.put(x, y, hexc("d9d3c0"))
    cv.ellipse(43, 24, 13, 6, hexc("3d2218")); cv.ellipse(43, 23, 11, 4.5, hexc("5a3324"))       # feijão
    for (x, y) in [(36, 22), (41, 21), (47, 23), (50, 25), (39, 25)]: cv.put(x, y, hexc("7a4630"))
    cv.ellipse(44, 36, 9, 5, hexc("7e3f22")); cv.ellipse(43, 35, 7, 3.5, hexc("a3592e"))           # bife
    cv.line(38, 35, 49, 35, hexc("6a3418"))
    cv.ellipse(59, 35, 5, 6, hexc("e0b453")); cv.noise(54, 30, 10, 11, [hexc("c99a3c")], 0.3, rng(3))  # farofa
    cv.ellipse(59, 22, 5, 4, hexc("4fa33a")); cv.put(57, 21, hexc("7fd05a")); cv.put(61, 23, hexc("d7372f"))  # salada e tomate
    # tempo: manchas e uma mosca
    cv.ellipse(28, 40, 3, 1.5, hexc("6f7a4a", 120)); cv.put(50, 18, hexc("1b1b1e")); cv.put(49, 17, hexc("9fb6cc", 180)); cv.put(51, 17, hexc("9fb6cc", 180))
    cv.outline(INK)
    return cv


def escopeta_grande():
    cv = Canvas(112, 34)
    cv.ellipse(56, 31, 50, 2.5, hexc("000000", 60))
    cv.rect(30, 8, 78, 6, hexc("4a4d55")); cv.hline(30, 8, 78, hexc("7b808a")); cv.hline(30, 13, 78, hexc("33363c"))
    cv.rect(46, 14, 40, 5, hexc("2d3036")); cv.hline(46, 14, 40, hexc("5d626b"))
    cv.rect(2, 8, 30, 14, C["wood"]); cv.hline(2, 8, 30, C["wood_l"]); cv.line(2, 21, 24, 27, C["wood_d"])
    cv.rect(4, 20, 20, 8, C["wood"]); cv.hline(4, 27, 20, C["wood_d"])
    cv.rect(50, 18, 24, 8, C["wood_d"]); cv.hline(50, 18, 24, C["wood"])
    cv.rect(32, 14, 6, 8, hexc("2a2b30")); cv.put(34, 20, hexc("8a8e95"))
    for x in range(60, 104, 8): cv.put(x, 10, hexc("9aa0a8"))
    cv.rect(104, 7, 4, 8, hexc("33363c"))
    cv.outline(INK)
    return cv


if __name__ == "__main__":
    for name, fn in {"cracha": cracha, "marmita": marmita, "escopeta": escopeta_item, "cafezinho": cafezinho}.items():
        fn().save(f"sprites/items/{name}.png".replace("sprites", "sprites", 1))
    for name, fn in {"laser": laser, "escopeta": escopeta, "canhao": canhao}.items():
        fn().save(f"sprites/weapons/{name}.png")
    for name, fn in {"bolt": bolt, "pellet": pellet, "orb": orb}.items():
        fn().save(f"sprites/fx/{name}.png")
    salgados().save("sprites/fx/salgados.png")
    for i in range(3): pilha(i).save(f"sprites/objects/pilha_{i}.png")
    sheet([alvo(0), alvo(1)], 2).save("sprites/objects/alvo.png")
    grade(False).save("sprites/objects/grade_h.png"); grade(True).save("sprites/objects/grade_v.png")
    catraca(True).save("sprites/objects/catraca_trancada.png"); catraca(False).save("sprites/objects/catraca_aberta.png")
    cracha_grande().save("sprites/items/inspecao/cracha.png")
    marmita_grande().save("sprites/items/inspecao/marmita.png")
    escopeta_grande().save("sprites/items/inspecao/escopeta.png")
    print("itens ok")
