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
| Entrada (nível de baixo) | praça de bloquete vermelho com gramados e mastros; a parte do prédio que se vê daqui é a ponta lisa, sem janelas: só o logo verde, o letreiro UNIFESP (sem o N e o S), o texto do campus e a faixa vertical perfurada |
| Portaria | volume branco com um grande vão (a catraca passa por ele) e a cabine de vidro escuro do porteiro; a laje fica transparente quando o jogador passa por baixo |
| Rampa | depois da catraca, rampa com ranhuras e guarda-corpo dos dois lados sobe para o pátio |
| Corredor (terraço elevado) | granito claro em placas; de um lado a fachada comprida do prédio (janelas de caixilho branco nos andares de cima, blocos e portas de vidro no térreo); do outro a borda com mureta e guarda-corpo, de onde se vê o estacionamento lá embaixo; canteiros com palmeiras, agaves e globos; o fundo é aberto, com gramado e a caixa d'água |
| Estacionamento (nível de baixo, só para ver) | um "L" como na foto aérea: começa na avenida (entrada própria, portão de carros fechado) e sobe acompanhando o terraço pelo lado de fora, com a faixa de bloquete na base do muro; sextavado com vagas, carros abandonados, postes de sódio |
| Em volta | a mata fechada atrás e à esquerda do prédio, com os anexos brancos engolidos por ela |
| RU | cerâmica bege, mesas brancas compridas com banquinhos fixos, cadeiras pretas "R.U.", bufê de aço, devolução de bandejas |
| Cantina | granito branco, vitrines de salgados, geladeira de bebidas, sinucas, mesas redondas com cadeiras pretas, lixeiras da coleta seletiva, bebedouro, sofá, mural |

## Arquivos

| Script | Gera |
|---|---|
| `pix.py` | ferramentas de desenho (retângulo, elipse, linha, contorno, fonte 3x5 para placas) |
| `palette.py` | todas as cores, com o nome de onde vieram |
| `tiles_fase1.py` | `sprites/tiles/fase1_tiles.png`, `tilesets/fase1_tileset*.tres` e `tools/levelgen/tiles_fase1.json` |
| `props_fase1.py` | `sprites/props/fase1/` (prédio, guarita, jardins, balcões, mesas, sinuca...) |
| `props_fase2.py` | `sprites/props/fase2/` (estantes, mesas, xadrez gigante, elevadores, armários, caixas, luminária), a Bibliotecária, os livros que ela joga, o livro e as chaves (com a arte de inspeção) |
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

## Chuva (Fase 1)

`objects/weather/rain.tscn` (`Rain`): chove a noite toda.

- **Pingos**: `rain.gdshader` desenha no mundo, em pixel art, três camadas de riscos inclinados, respingos no chão e anéis
  nas poças. Escurecem com a noite e brilham na lanterna e perto dos postes. A chuva é dividida em pedaços de 6x6 tiles para
  cada pedaço receber só as luzes de perto.
- **Onde chove**: o gerador da fase grava `levels/fase_1_chuva.png` (1 pixel por tile: R = chove, G = respinga, B = poça).
  Dentro do RU, da cantina e do elevador não chove; só pinga nas poças, do teto furado.
- **Relâmpagos**: a cada 14 a 32 s o `CanvasModulate` da noite clareia de repente (às vezes duas vezes) e o trovão vem
  depois: quase junto quando o raio cai perto, segundos depois quando é longe. Dentro das salas o clarão é mais fraco.
- **Som**: `sounds/clima/` (`chuva.ogg` em loop e três trovões), gerados por `tools/audio/gen_chuva.py` (ruído filtrado e
  gotas sintetizadas, precisa de numpy, scipy e ffmpeg). Tocam no bus `Chuva`, criado pelo script, que abafa o som com um
  filtro passa-baixa quando o jogador entra numa sala.

## Fase 2: o andar da biblioteca

Referência: as fotos da biblioteca e do corredor da frente dela no tour 360°. A Fase 2 usa o mesmo tileset da Fase 1
(linhas 22 a 24 do atlas) e o gerador `tools/levelgen/gen_fase2.py` monta a arte por cima da planta do time
(`L.tiled_layers` em `lib.py`). A planta, as salas e o roteiro não mudaram.

| Lugar no jogo | O que veio do campus de verdade |
|---|---|
| Corredor e Hall das Caronas | granito branco polido; paredes cobertas do começo ao fim de mãos pintadas com tinta, cada uma com o nome do formando embaixo (`objects/decor/Signatures.cs` nas paredes laterais, tiles `maos` nas de frente); extintor, venezianas e placas de SAÍDA; o fundo do corredor é um janelão (com a chuva escorrendo) |
| Entrada da biblioteca | quadro verde de avisos com a plaquinha BIBLIOTECA, guarda-volumes azuis, caixa de devolução de madeira |
| Biblioteca | piso vinílico bege; estantes de aço bege com livros coloridos; mesas brancas com cadeiras pretas; mesas redondas; balcão de atendimento com os computadores; o mural colorido de rostos na parede de cima; o xadrez gigante com o tapete azul e branco; placas amarelas de piso molhado |
| Salas de estudo | janelões para fora e a luminária verde de mesa |
| Achados e Perdidos | lousa verde, armário de madeira, caixas de papelão (fechada, amassada, aberta) |

- **Noite e chuva**: a mesma escuridão da Fase 1, fluorescentes falhando, luzes de emergência vermelhas (elevador quebrado,
  porta da biblioteca) e a placa verde da escada. A chuva fica do lado de fora: só se ouve abafada (`Rain.AlwaysInside`)
  e os relâmpagos clareiam o andar inteiro.
- **Bibliotecária**: fantasma de coque e óculos que brilham, cardigã roxo e a saia que vira névoa. Quadros: andando
  (fazendo "shhh"), jogando livro, caçando (cabelo solto e olhos vermelhos) e tonta (óculos tortos, livro na cabeça).
  Ela e o cone da lanterna dela aparecem no escuro. Os livros que ela joga são `sprites/fx/livros.png`.
- **A luta (furtividade com barulho)**: ela enxerga pouco mas ouve tudo (`objects/noise/Noise.cs`, com a onda do som
  desenhada no chão). Um tiro na estante bamba (`objects/librarian/ToppleShelf.cs`) faz ela balançar e derrubar livros:
  a Bibliotecária vem ver. Quando ela parar na frente, outro tiro derruba a estante em cima dela (`estante_caida.png`).
  Três estantes e ela cai; tonta, ela também leva tiro. Se te vê, caça e joga livros até você sumir da vista dela.
  Não tem fantasma na biblioteca.
- **Abandono**: livros caídos das estantes, papéis, poças das goteiras, forro caído, peças do xadrez tombadas.

## Inspeção de itens

Ao pegar um item com `InspectTexture` (crachá, marmita, escopeta; na Fase 2, a chave dos Achados, o Cálculo I e a chave da escada), `objects/inspect/item_inspect.tscn` pausa o jogo e mostra
a arte grande (`sprites/items/inspecao/`), balançando com um brilho passando, com nome e descrição ao lado, como no
Resident Evil. O crachá segue o modelo real da UNIFESP SJC (cordão verde, logo, faixa "SÃO JOSÉ DOS CAMPOS"), com foto
apagada e nome fictício.

## Fonte

`fonts/PixelifySans.ttf` (licença SIL Open Font License, em `fonts/PixelifySans-OFL.txt`) é a fonte padrão do projeto.
