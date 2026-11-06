// Brilho passando pelo item (como a luz batendo no plástico do crachá).
shader_type canvas_item;
uniform float shine_pos = 0.0;
void fragment() {
	vec4 c = texture(TEXTURE, UV);
	float d = UV.x + UV.y * 0.45 - shine_pos;
	float band = smoothstep(0.07, 0.0, abs(d));
	c.rgb += band * 0.4 * c.a;
	COLOR = c * COLOR;
}
