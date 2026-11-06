# Tira fotos de uma fase em pontos escolhidos (para conferir a arte sem jogar).
# Uso: godot --rendering-driver opengl3 --resolution 1152x648 --script res://tools/capture/capture.gd -- <cena> <prefixo> <ponto> ...
# Cada ponto: "x,y" ou "x,y,ação[,ação...]". Ações:
#   ghosts       três fantasmas (normal, rápido, pesado) perto do jogador
#   boss:Caminho chama StartFight() no chefe (ex.: boss:Cantina/Tia)
#   mouse:dx:dy  mira (posição do mouse relativa ao centro da tela)
#   wait:n       espera n quadros a mais antes da foto
#   hud          mostra o HUD nesta foto
extends SceneTree

func _initialize():
	var args := OS.get_cmdline_user_args()
	var scene: String = args[0]
	var prefix: String = args[1]
	var points := args.slice(2)
	change_scene_to_file(scene)
	for i in 32:
		await process_frame
	var level := current_scene
	var player: Node2D = level.get_node("Player")
	var cam: Camera2D = player.get_node("Camera2D")
	var hud = level.get_node_or_null("Hud")
	var idx := 0
	for p in points:
		var parts: PackedStringArray = p.split(",")
		var extra_wait := 20
		if hud: hud.visible = false
		player.global_position = Vector2(float(parts[0]), float(parts[1]))
		cam.reset_smoothing()
		var center := Vector2(root.get_viewport().get_visible_rect().size / 2)
		Input.warp_mouse(center + Vector2(220, 40))
		for a in parts.slice(2):
			if a == "ghosts":
				var enemy_scene: PackedScene = load("res://objects/enemy/enemy.tscn")
				var kinds := [0, 1, 2]
				for k in kinds.size():
					var e = enemy_scene.instantiate()
					e.set("Type", kinds[k])
					e.global_position = player.global_position + Vector2(-260 + k * 170, -170 + (k % 2) * 60)
					level.add_child(e)
			elif a.begins_with("boss:"):
				level.get_node(a.substr(5)).call("StartFight")
			elif a.begins_with("mouse:"):
				var m := a.split(":")
				Input.warp_mouse(center + Vector2(float(m[1]), float(m[2])))
			elif a.begins_with("wait:"):
				extra_wait += int(a.substr(5))
			elif a == "hud" and hud:
				hud.visible = true
		for i in extra_wait:
			await process_frame
		await RenderingServer.frame_post_draw
		var img := root.get_viewport().get_texture().get_image()
		img.save_png("%s_%02d.png" % [prefix, idx])
		idx += 1
	quit()
