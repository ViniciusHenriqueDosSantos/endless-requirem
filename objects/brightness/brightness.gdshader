shader_type canvas_item;

// Clareia ou escurece a tela inteira (opção "Brilho" do menu). 1.0 = imagem original.
uniform sampler2D screen : hint_screen_texture, filter_nearest;
uniform float brightness = 1.0;

void fragment() {
	vec3 color = texture(screen, SCREEN_UV).rgb;
	// acima de 1: levanta mais as partes escuras (o jogo é noturno), sem estourar as claras
	vec3 lifted = brightness >= 1.0 ? pow(color, vec3(1.0 / brightness)) : color * brightness;
	COLOR = vec4(lifted, 1.0);
}
