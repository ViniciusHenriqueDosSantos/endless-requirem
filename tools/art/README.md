# Pixel art do jogo

Toda a arte é gerada por estes scripts Python a partir de uma paleta e de desenhos feitos em código.
Assim dá para ajustar uma cor ou um detalhe e regenerar tudo de uma vez, sem editar PNG na mão.

```
python tools/art/build_all.py      # gera todos os PNGs e remonta levels/fase_1.tscn
```

Precisa de Python 3 com Pillow (`pip install pillow`). Depois de rodar, abra a Godot: ela importa os PNGs novos sozinha.

## Regra de escala

**1 pixel da arte = 4 pixels do jogo.** O tile tem 16 px no PNG e 64 px no jogo. Personagens, props e itens
seguem a mesma regra (o estudante tem 16x24 px, ou seja, 64x96 no jogo). O filtro de textura do projeto é
"nearest", então nada fica borrado. Ao desenhar algo novo, faça no tamanho do `Size` do prop dividido por 4.

## Referência

As cores e os objetos vieram das fotos do tour 360° da UNIFESP São José dos Campos
(<https://pro360.com.br/interativo/unifesp/sao-jose-campos/>). As fotos **não** ficam no repositório.

| Lugar no jogo | O que veio do campus de verdade |
|---|---|
| Calçada e portão | bloquete vermelho em espinha de peixe, faixa tátil amarela, meio-fio branco, gradil verde, ponto de ônibus verde |
| Entrada | piso sextavado com grama nas juntas, fachada de concreto bege com faixa de janelas e o letreiro UNIFESP (sem o N e o S) |
| Portaria | guarita de concreto com janelão, catraca de aço com luz vermelha/verde |
| Corredor (praça) | granito claro em placas, canteiros brancos com palmeiras e agaves, postes de globo, caixa d'água verde |
| RU | cerâmica bege, mesas brancas compridas com banquinhos fixos, cadeiras pretas "R.U.", bufê de aço, devolução de bandejas |
| Cantina | granito branco, vitrines de salgados, geladeira de bebidas, sinucas, mesas redondas com cadeiras pretas, lixeiras da coleta seletiva, bebedouro, sofá, mural |

## Arquivos

| Script | Gera |
|---|---|
| `pix.py` | ferramentas de desenho (retângulo, elipse, linha, contorno, fonte 3x5 para placas) |
| `palette.py` | todas as cores, com o nome de onde vieram |
| `tiles_fase1.py` | `sprites/tiles/fase1_tiles.png`, `tilesets/fase1_tileset*.tres` e `tools/levelgen/tiles_fase1.json` |
| `props_fase1.py` | `sprites/props/fase1/` (prédio, guarita, jardins, balcões, mesas, sinuca...) |
| `characters.py` | `sprites/characters/` (estudante, 3 fantasmas, Tia da Cantina) |
| `items.py` | `sprites/items/`, `weapons/`, `fx/`, `objects/` (crachá, marmita, armas, tiros, salgados, portas, catraca, pilhas de papel, alvos) |
| `preview.py`, `sheet_preview.py` | pranchas ampliadas para revisar a arte fora do jogo |

## Como o tileset funciona

O gerador da fase (`tools/levelgen/gen_fase1.py`) decide o **material** de cada célula de chão (bloquete, sextavado,
praça, granito, cerâmica, asfalto...). `tools/levelgen/tiling.py` escolhe o tile:

- **piso**: variações sorteadas (sempre o mesmo sorteio para a mesma célula);
- **paredes**: o estilo vem do chão em volta (interno = parede branca, fachada = concreto com janelas, externo = cerca viva
  com gradil) e o desenho do topo depende de quais vizinhos também são parede (autotile de 16 combinações);
- **sombras**: uma camada por cima do piso escurece o chão encostado nas paredes;
- **fundo**: o vazio fora das salas vira grama (lado de fora) ou laje (telhado dos prédios).

## Personagens no código

- `Player`: a folha `estudante.png` tem 4 colunas (parado + passos) e 3 linhas (frente, costas, lado). O corpo vira para onde
  o mouse mira, e a arma (`Weapon.Sprite`) aparece na mão.
- `Enemy`: um sprite por tipo (`NormalSprite`, `FastSprite`, `TankSprite` em `enemy.tscn`), 4 quadros flutuando.
- `Boss`: `BossConfig.Sprite` com 4 quadros (parada, parada, arremessando, tonta) e `ProjectileSprite` (salgados sorteados).
- Tudo que ganhou sprite continua funcionando sem ele: se o campo estiver vazio, aparece o desenho provisório antigo.
  Por isso a Fase 2 segue igual até ganhar a arte dela.

## Fonte

`fonts/PixelifySans.ttf` (licença SIL Open Font License, em `fonts/PixelifySans-OFL.txt`) é a fonte padrão do projeto.
