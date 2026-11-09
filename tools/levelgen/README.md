# Geradores de planta

As cenas `levels/fase_1.tscn`, `levels/fase_2.tscn` e `levels/escada.tscn` são geradas por estes scripts
(chão, paredes, salas, portas, móveis e coletáveis). Se alguém editar a cena à mão na Godot e depois rodar o
gerador de novo, a edição manual se perde — combine antes.

```
python3 tools/levelgen/gen_fase1.py  levels/fase_1.tscn
python3 tools/levelgen/gen_fase2.py  levels/fase_2.tscn
python3 tools/levelgen/gen_escada.py levels/escada.tscn
python3 tools/levelgen/render_map.py levels/fase_2.tscn planta.png   # imagem da planta vista de cima (precisa de Pillow)
python3 tools/levelgen/check_walls.py levels/fase_1.tscn levels/fase_2.tscn levels/escada.tscn   # confere as paredes
```

**Paredes:** `check_walls.py` confere se dá para atravessar alguma parede: todo tile das camadas `Walls` e
`OuterWalls` precisa ter colisão no tileset, e todo chão só pode encostar em chão ou em parede sólida.
Os geradores rodam essa conferência no fim e param com erro se algo falhar. Ao criar um tile novo de parede
na arte (`tools/art/tiles_fase1.py`), o nome dele precisa começar com um dos `WALL_PREFIXES`.

A pasta `tools/` tem um arquivo `.gdignore` para a Godot não importar nada daqui.
