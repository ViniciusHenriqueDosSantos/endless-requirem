// Chuva vista de cima, em pixel art (1 pixel da arte = 4 pixels do jogo).
// Desenhada no mundo, por cima de tudo. Uma máscara (1 pixel por tile) diz onde chove:
//   R = chove ali (lado de fora); G = quanto respinga no chão; B = poça (anéis na água).
// É afetada pela escuridão e pelas luzes: os pingos brilham na lanterna e perto dos postes.
shader_type canvas_item;

uniform sampler2D mask : filter_nearest, repeat_disable;
uniform vec2 mask_origin = vec2(0.0);   // tile do pixel (0,0) da máscara
uniform float tile_size = 64.0;
uniform float pixel = 4.0;
uniform vec4 rain_color : source_color = vec4(0.72, 0.82, 1.0, 1.0);
uniform float boost = 4.0;              // compensa o CanvasModulate escuro (a C# ajusta)
uniform float intensity = 1.0;          // 0..1 (rajadas)
uniform float slant = 0.22;             // inclinação dos pingos (vento)

varying vec2 world;

void vertex() {
	world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

float hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

vec3 mask_at(vec2 art) {
	vec2 tile = floor(art * pixel / tile_size) - mask_origin;
	vec2 size = vec2(textureSize(mask, 0));
	if (tile.x < 0.0 || tile.y < 0.0 || tile.x >= size.x || tile.y >= size.y) return vec3(1.0, 0.4, 0.0);
	return texture(mask, (tile + 0.5) / size).rgb;
}

// Uma camada de riscos caindo. p = pixel da arte. Retorna o brilho (0..1).
float streaks(vec2 p, float t, vec2 cell, float speed, float len, float density, float seed) {
	float y = p.y - floor(t * speed);
	float x = p.x + floor(p.y * slant);   // desloca com o y do mundo: o risco é inclinado e cai na diagonal
	vec2 id = floor(vec2(x, y) / cell);
	vec2 f = vec2(x, y) - id * cell;
	if (hash(id + seed) > density * intensity) return 0.0;
	float x0 = floor(hash(id + seed + 3.1) * (cell.x - 1.0));
	float y0 = floor(hash(id + seed + 7.7) * (cell.y - len));
	if (f.x != x0 || f.y < y0 || f.y >= y0 + len) return 0.0;
	return 0.35 + 0.65 * (f.y - y0 + 1.0) / len;   // a ponta de baixo (a gota) é mais forte
}

// Respingos: um ponto que vira uma cruz e depois um anel, em lugares sorteados do chão.
// chance: vem da máscara do próprio pixel (G ou B), lida uma vez só no fragment().
float splash(vec2 p, float t, float cell, float rate, float seed, float amount, bool puddle, float chance) {
	vec2 id = floor(p / cell);
	float r = hash(id + seed);
	float period = 1.0 / rate;
	float ph = fract(t / period + r);
	float cycle = floor(t / period + r);
	if (hash(id + seed + cycle * 0.37) > amount * chance * intensity) return 0.0;  // cada ciclo cai num lugar diferente
	vec2 c = id * cell + floor(vec2(hash(id + seed + 1.3), hash(id + seed + 2.9)) * (cell - 4.0)) + 2.0;
	vec2 d = p - c;
	if (puddle) {  // anel achatado (perspectiva) que cresce e some
		float k = ph / 0.55;
		if (k > 1.0) return 0.0;
		float rad = 1.0 + floor(k * 4.0);
		float e = length(vec2(d.x / 2.0, d.y)) ;
		return (abs(e - rad * 0.5) < 0.5) ? (1.0 - k) * 0.9 : 0.0;
	}
	float k = ph / 0.22;
	if (k > 1.0) return 0.0;
	if (k < 0.33) return (d == vec2(0.0)) ? 1.0 : 0.0;
	if (k < 0.66) return (abs(d.x) + abs(d.y) == 1.0) ? 0.8 : 0.0;
	return (abs(d.x) == 2.0 && d.y == 0.0) || (d.x == 0.0 && d.y == -1.0) ? 0.45 : 0.0;
}

void fragment() {
	vec2 p = floor(world / pixel);
	vec3 m = mask_at(p);   // a única leitura de textura
	if (m.r <= 0.0 && m.b <= 0.0) discard;   // seco (dentro do prédio)
	float t = TIME;
	float a = 0.0;
	if (m.r > 0.0) {
		a = max(a, streaks(p, t, vec2(7.0, 30.0), 150.0, 6.0, 0.55, 0.0) * 0.55);   // perto: riscos longos
		a = max(a, streaks(p, t, vec2(5.0, 22.0), 115.0, 4.0, 0.5, 17.0) * 0.4);    // meio
		a = max(a, streaks(p, t, vec2(4.0, 15.0), 80.0, 2.0, 0.45, 41.0) * 0.25);   // longe: tracinhos
		if (m.g > 0.0) a = max(a, splash(p, t, 9.0, 1.6, 5.0, 0.6, false, m.g) * 0.55);
		a *= m.r;
	}
	if (m.b > 0.0) a = max(a, splash(p, t, 5.0, 1.1, 23.0, 0.9, true, m.b) * 0.5);  // poças (também dentro, onde o teto pinga)
	if (a <= 0.0) discard;   // a maior parte da tela: nada para misturar nem iluminar
	COLOR = vec4(rain_color.rgb * boost, a * rain_color.a);
}
