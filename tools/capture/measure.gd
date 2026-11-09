# Mede o desempenho de uma fase: tempo de quadro e chamadas de desenho em alguns pontos.
# Uso (Linux com xvfb): xvfb-run -a godot --rendering-driver opengl3 --resolution 1152x648 \
#   --script res://tools/capture/measure.gd -- res://levels/fase_1.tscn full 1152,4100 1730,1300
# Modos: full (tudo), norain (sem chuva), nolights (sem luzes nem lanterna), nonight (sem luzes, escuridão e tela da noite).
# Comparar os modos mostra quanto cada coisa custa.
extends SceneTree
func _initialize():
	var args := OS.get_cmdline_user_args()
	change_scene_to_file(args[0])
	for i in 40: await process_frame
	var level := current_scene
	var mode: String = args[1]
	if mode != "full" and mode != "norain":
		var l = level.get_node_or_null("Luzes")
		if l: l.queue_free()
		var p = level.get_node_or_null("Player")
		for n in ["Lanterna", "Halo"]:
			var x = p.get_node_or_null(n)
			if x: x.queue_free()
	if mode == "norain":
		level.get_node("Chuva").queue_free()
	if mode == "nonight":
		for n in ["Noite", "NightOverlay"]:
			var x = level.get_node_or_null(n)
			if x: x.queue_free()
	var player: Node2D = level.get_node("Player")
	var total_ms := 0.0
	var total_dc := 0
	var n := 0
	for pt in args.slice(2):
		var xy: PackedStringArray = pt.split(",")
		player.global_position = Vector2(float(xy[0]), float(xy[1]))
		for i in 30:
			player.call("HealFull")
			await process_frame
		var t0 := Time.get_ticks_usec()
		for i in 60:
			player.call("HealFull")
			await process_frame
		var ms := (Time.get_ticks_usec() - t0) / 60000.0
		var dc := RenderingServer.get_rendering_info(RenderingServer.RENDERING_INFO_TOTAL_DRAW_CALLS_IN_FRAME)
		var obj := RenderingServer.get_rendering_info(RenderingServer.RENDERING_INFO_TOTAL_OBJECTS_IN_FRAME)
		print("PONTO %s  ms/quadro=%.1f  draw_calls=%d  objetos=%d  process=%.2fms physics=%.2fms nodes=%d" % [pt, ms, dc, obj, Performance.get_monitor(Performance.TIME_PROCESS)*1000, Performance.get_monitor(Performance.TIME_PHYSICS_PROCESS)*1000, Performance.get_monitor(Performance.OBJECT_NODE_COUNT)])
		total_ms += ms; total_dc += dc; n += 1
	print("MEDIA ms=%.1f draw_calls=%d" % [total_ms / n, total_dc / n])
	quit()
