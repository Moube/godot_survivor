shader_type canvas_item;

uniform bool flash_enabled = false;
uniform vec4 flash_color : source_color = vec4(1.0, 0.0, 0.0, 1.0);

void fragment() {
	vec4 tex = texture(TEXTURE, UV);
	vec4 tinted = tex * COLOR;

	if (flash_enabled && tex.a > 0.0) {
		COLOR = vec4(flash_color.rgb, tinted.a * flash_color.a);
	} else {
		COLOR = tinted;
	}
}
