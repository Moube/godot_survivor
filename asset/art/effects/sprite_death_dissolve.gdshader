shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 edge_color : source_color = vec4(1.0, 0.9, 0.28, 1.0);
uniform vec4 burn_color : source_color = vec4(1.0, 0.22, 0.02, 1.0);
uniform float noise_scale = 28.0;
uniform float edge_width : hint_range(0.01, 0.4) = 0.12;
uniform float vertical_bias : hint_range(-1.0, 1.0) = 0.18;

float hash21(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);

	float a = hash21(i);
	float b = hash21(i + vec2(1.0, 0.0));
	float c = hash21(i + vec2(0.0, 1.0));
	float d = hash21(i + vec2(1.0, 1.0));

	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	if (tex.a <= 0.001) {
		COLOR = vec4(0.0);
	} else {
		vec4 base = tex * COLOR;
		float n1 = value_noise(UV * noise_scale);
		float n2 = value_noise((UV * noise_scale * 0.45) + vec2(11.7, 4.3));
		float dissolve_noise = mix(n1, n2, 0.35);
		float shaped_noise = dissolve_noise + ((UV.y - 0.5) * vertical_bias);
		float threshold = (progress * 1.18) - 0.1;

		float alive = step(threshold, shaped_noise);
		float edge = 1.0 - smoothstep(0.0, edge_width, abs(shaped_noise - threshold));
		edge *= smoothstep(0.02, 0.18, progress) * (1.0 - smoothstep(0.82, 1.0, progress));

		vec3 glow = mix(burn_color.rgb, edge_color.rgb, clamp(edge * 1.4, 0.0, 1.0));
		base.rgb = mix(base.rgb, glow, clamp(edge, 0.0, 0.9));
		base.a *= max(alive, edge * 0.72);

		if (base.a <= 0.01) {
			COLOR = vec4(0.0);
		} else {
			COLOR = base;
		}
	}
}
