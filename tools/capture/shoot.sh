#!/bin/sh
# Fotos de uma fase rodando de verdade (Linux com xvfb). Ex.: tools/capture/shoot.sh res://levels/fase_1.tscn /tmp/cap 1152,4100 800,900
cd "$(dirname "$0")/../.."
scene="$1"; prefix="$2"; shift 2
xvfb-run -a -s "-screen 0 1152x648x24" godot --rendering-driver opengl3 --resolution 1152x648 \
  --script res://tools/capture/capture.gd -- "$scene" "$prefix" "$@" 2>&1 | grep -E "^(ERROR|SCRIPT ERROR)" | grep -v "audio\|ALSA\|status < 0" | head -20
