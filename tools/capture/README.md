# Fotos automáticas de uma fase

`capture.gd` abre uma cena, coloca o jogador em cada ponto pedido e salva uma foto da tela. Serve para revisar a arte
sem precisar jogar até lá.

```
godot --rendering-driver opengl3 --resolution 1152x648 --script res://tools/capture/capture.gd -- res://levels/fase_1.tscn fotos/f1 1152,4110,hud 830,900,boss:Cantina/Tia
```

Cada ponto é `x,y` (posição do jogador em pixels) e pode ter ações depois: `ghosts` (três fantasmas perto),
`boss:Caminho/DoChefe` (começa a luta), `mouse:dx:dy` (mira), `wait:n` (espera mais n quadros), `hud` (mostra o HUD).
No Linux sem tela, `shoot.sh` faz o mesmo usando `xvfb-run`.

## Medir desempenho

`measure.gd` põe o jogador em cada ponto e mede o tempo médio de quadro e as chamadas de desenho. Rodando com um modo
diferente (`full`, `norain`, `nolights`, `nonight`) dá para ver quanto custa a chuva, as luzes e a tela da noite.

```
xvfb-run -a godot --rendering-driver opengl3 --resolution 1152x648 --script res://tools/capture/measure.gd -- res://levels/fase_1.tscn full 1152,4100 1730,1300
```

O que já foi otimizado (Fase 1, renderização por software, média de 4 pontos: 60 ms → 41 ms por quadro):

- chuva: uma leitura da máscara por pixel e `discard` onde não tem pingo (o pixel vazio não passa pelas luzes);
- luzes apagadas (`FlickerLight` com energia ~0, como as queimadas) ficam desligadas e não entram na iluminação;
- o zumbido das lâmpadas só toca (e só é decodificado) com o jogador perto;
- fantasmas, tiros e itens usam um material só (`SharedMaterials.Unshaded`), e a Godot desenha tudo junto;
- props só rodam `_Process` quando somem com o jogador atrás (`FadeWhenBehind`).
