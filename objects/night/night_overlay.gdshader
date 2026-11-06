// Clima da noite: vinheta escura nas bordas, granulado de filme e uma neblina bem leve se mexendo.
shader_type canvas_item;

uniform vec4 tint : source_color = vec4(0.02, 0.03, 0.06, 1.0);
uniform float vignette = 0.75;
uniform float grain = 0.07;
uniform float fog = 0.05;
uniform float pulse = 0.0; // 0..1: borda avermelhada (vida baixa)

float hash(vec2 p) { return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453); }

void fragment() {
	vec2 uv = UV;
	float d = distance(uv, vec2(0.5)) * 1.25;
	float v = smoothstep(0.38, 1.0, d) * vignette;
	float n = hash(floor(uv * vec2(288.0, 162.0)) + floor(TIME * 24.0)) - 0.5;
	float f = sin(uv.x * 5.0 + TIME * 0.13) + sin(uv.y * 4.0 - TIME * 0.09) + sin((uv.x - uv.y) * 7.0 + TIME * 0.05);
	vec3 col = mix(tint.rgb, vec3(0.45, 0.02, 0.03), pulse * smoothstep(0.45, 1.0, d));
	float a = clamp(v + n * grain + f * fog * 0.33 + pulse * smoothstep(0.5, 1.0, d) * 0.35, 0.0, 0.97);
	COLOR = vec4(col, a);
}
