# Gera toda a pixel art do jogo e remonta as Fases 1 e 2 com ela.
# Uso (na raiz do projeto): python tools/art/build_all.py
# Precisa de Python 3 com Pillow (pip install pillow).
import os, runpy, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
os.chdir(HERE)
sys.path.insert(0, HERE)

for script in ["tiles_fase1.py", "props_fase1.py", "props_fase2.py", "characters.py", "items.py", "lights.py"]:
    print("->", script)
    cwd = os.getcwd()
    os.chdir(ROOT if script == "items.py" else HERE)
    runpy.run_path(os.path.join(HERE, script), run_name="__main__")
    os.chdir(cwd)

print("-> gen_fase1.py")
subprocess.run([sys.executable, os.path.join(ROOT, "tools", "levelgen", "gen_fase1.py"),
                os.path.join(ROOT, "levels", "fase_1.tscn")], check=True)
print("-> gen_fase2.py")
subprocess.run([sys.executable, os.path.join(ROOT, "tools", "levelgen", "gen_fase2.py"),
                os.path.join(ROOT, "levels", "fase_2.tscn")], check=True)
print("Pronto. Abra a Godot para ela importar os PNGs novos.")
