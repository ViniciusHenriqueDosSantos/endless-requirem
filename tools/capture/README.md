# Fotos automáticas de uma fase

`capture.gd` abre uma cena, coloca o jogador em cada ponto pedido e salva uma foto da tela. Serve para revisar a arte
sem precisar jogar até lá.

```
godot --rendering-driver opengl3 --resolution 1152x648 --script res://tools/capture/capture.gd -- res://levels/fase_1.tscn fotos/f1 1152,4110,hud 830,900,boss:Cantina/Tia
```

Cada ponto é `x,y` (posição do jogador em pixels) e pode ter ações depois: `ghosts` (três fantasmas perto),
`boss:Caminho/DoChefe` (começa a luta), `mouse:dx:dy` (mira), `wait:n` (espera mais n quadros), `hud` (mostra o HUD).
No Linux sem tela, `shoot.sh` faz o mesmo usando `xvfb-run`.
