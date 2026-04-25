shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform vec4 shadow_color : source_color = vec4(0.14, 0.16, 0.13, 0.38);
uniform vec2 contact_center = vec2(0.22, 0.52);
uniform vec2 contact_radius = vec2(0.16, 0.12);
uniform float contact_angle = 0.0;
uniform float contact_strength : hint_range(0.0, 2.0, 0.01) = 1.0;
uniform vec2 cast_center = vec2(0.62, 0.58);
uniform vec2 cast_radius = vec2(0.46, 0.28);
uniform float cast_angle = 0.35;
uniform float cast_strength : hint_range(0.0, 2.0, 0.01) = 0.72;
uniform float procedural_softness : hint_range(0.01, 0.95, 0.01) = 0.42;

float ellipse_mask(vec2 uv, vec2 center, vec2 radius, float angle) {
	vec2 safe_radius = max(radius, vec2(0.001));
	float c = cos(angle);
	float s = sin(angle);
	vec2 delta = uv - center;
	vec2 rotated = vec2(
		c * delta.x + s * delta.y,
		-s * delta.x + c * delta.y);
	vec2 p = rotated / safe_radius;
	float dist = dot(p, p);
	float mask = clamp(1.0 - dist, 0.0, 1.0);
	return pow(mask, max(0.2, 1.0 / max(procedural_softness, 0.01)));
}

void fragment() {
	float contact = ellipse_mask(UV, contact_center, contact_radius, contact_angle) * contact_strength;
	float cast = ellipse_mask(UV, cast_center, cast_radius, cast_angle) * cast_strength;
	float mask = clamp(max(contact, cast), 0.0, 1.0);
	COLOR = vec4(shadow_color.rgb, shadow_color.a * mask);
}
