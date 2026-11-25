shader_type canvas_item;

uniform sampler2D text_tex : source_color, filter_linear;
uniform float scroll = 0.0;
uniform float bottom_width = 1.0;
uniform float tilt = 0.7;
uniform float depth_scale = 0.35;
uniform float fade_start = 0.6;
uniform float fade_end = 0.9;

void fragment() {
	float height = 1.0 - UV.y;
	float shrink = 1.0 - height * tilt;
	float depth = height / shrink;
	vec2 page = vec2((UV.x - 0.5) / (bottom_width * shrink) + 0.5, scroll - depth * depth_scale);
	float inside = step(0.0, page.x) * step(page.x, 1.0) * step(0.0, page.y) * step(page.y, 1.0);
	float fade = 1.0 - smoothstep(fade_start, fade_end, height);
	vec4 text = texture(text_tex, page);
	COLOR = vec4(text.rgb, text.a * inside * fade);
}
