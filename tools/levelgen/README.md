# Geradores de planta

As cenas `levels/fase_1.tscn`, `levels/fase_2.tscn` e `levels/escada.tscn` são geradas por estes scripts
(chão, paredes, salas, portas, móveis e coletáveis). Se alguém editar a cena à mão na Godot e depois rodar o
gerador de novo, a edição manual se perde — combine antes.

```
python3 tools/levelgen/gen_fase1.py  levels/fase_1.tscn
python3 tools/levelgen/gen_fase2.py  levels/fase_2.tscn
python3 tools/levelgen/gen_escada.py levels/escada.tscn
python3 tools/levelgen/render_map.py levels/fase_2.tscn planta.png   # imagem da planta vista de cima (precisa de Pillow)
```

A pasta `tools/` tem um arquivo `.gdignore` para a Godot não importar nada daqui.
