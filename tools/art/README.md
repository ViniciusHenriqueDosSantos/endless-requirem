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
| Entrada (nível de baixo) | praça de bloquete vermelho com gramados e mastros; a ponta do prédio é lisa (só logo verde, letreiro UNIFESP sem o N e o S, texto do campus e a faixa vertical perfurada); a face comprida, recuada, tem as janelas por andar |
| Portaria | volume branco com um grande vão (a catraca passa por ele) e a cabine de vidro escuro do porteiro; a laje fica transparente quando o jogador passa por baixo |
| Rampa | depois da catraca, rampa com ranhuras e guarda-corpo dos dois lados sobe para o pátio; à direita, lá embaixo, o estacionamento |
| Corredor (pátio entre duas alas) | granito claro em placas; as duas fachadas com janelas de caixilho branco nos andares de cima e blocos de vidro/portas de vidro no térreo; canteiros com palmeiras, agaves e globos junto às fachadas e no meio; o fundo é aberto, com mureta, gramado e a caixa d'água |
| Estacionamento (nível de baixo, só para ver) | ao lado da rampa: faixa de bloquete junto ao muro, sextavado com vagas, carros abandonados, postes de sódio |
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
| `lights.py` | `sprites/lights/` (texturas das luzes) |
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

- `Player`: a folha `estudante.png` tem 4 colunas (parado + passos) e 4 linhas (frente, costas, lado, cambalhota). O corpo vira para onde
  o mouse mira, e a arma (`Weapon.Sprite`) aparece na mão.
- `Enemy`: um sprite por tipo (`NormalSprite`, `FastSprite`, `TankSprite` em `enemy.tscn`), 4 quadros flutuando.
- `Boss`: `BossConfig.Sprite` com 4 quadros (parada, parada, arremessando, tonta) e `ProjectileSprite` (salgados sorteados).
- Tudo que ganhou sprite continua funcionando sem ele: se o campo estiver vazio, aparece o desenho provisório antigo.
  Por isso a Fase 2 segue igual até ganhar a arte dela.

## Noite e abandono (Fase 1)

O jogo se passa em 2061, à noite, com o campus abandonado há décadas.

- **Escuridão**: um `CanvasModulate` azul-escuro na fase, no grupo `night`. Quando ele existe, o jogador acende a
  lanterna (cone que segue a mira e falha com pouca vida) e um halo fraco em volta (`Player.FlashlightTexture/AuraTexture`).
- **Luzes**: `objects/night/flicker_light.tscn` (`FlickerLight`): estável, piscando, queimada (só estalos) ou pulsando
  (emergência). O gerador põe os postes de sódio da avenida, os globos dos canteiros, as fluorescentes do RU e da cantina,
  a geladeira, a TV da guarita, a luz vermelha do elevador e as placas verdes de SAÍDA.
- **Tela**: `objects/night/night_overlay.tscn` (vinheta, granulado e neblina; a borda pulsa em vermelho com pouca vida).
- **Brilham no escuro**: fantasmas, tiros, salgados, itens e o café (material *unshaded*: ignoram a escuridão).
- **Abandono**: pisos com sujeira e mato; uma camada `Abandono` com rachaduras, folhas, mato, poças, papéis, cacos e forro
  caído; paredes com vidro quebrado, mofo, rachaduras, pichações e trepadeira; carros abandonados, cadeiras caídas, entulho.
- Texturas de luz: `lights.py` gera `sprites/lights/` (radial, cone da lanterna, tubo fluorescente).

## Inspeção de itens

Ao pegar um item com `InspectTexture` (crachá, marmita, escopeta), `objects/inspect/item_inspect.tscn` pausa o jogo e mostra
a arte grande (`sprites/items/inspecao/`), balançando com um brilho passando, com nome e descrição ao lado, como no
Resident Evil. O crachá segue o modelo real da UNIFESP SJC (cordão verde, logo, faixa "SÃO JOSÉ DOS CAMPOS"), com foto
apagada e nome fictício.

## Fonte

`fonts/PixelifySans.ttf` (licença SIL Open Font License, em `fonts/PixelifySans-OFL.txt`) é a fonte padrão do projeto.
