shader_type spatial;
render_mode blend_mix,depth_draw_opaque,cull_disabled,diffuse_burley,specular_schlick_ggx;
uniform vec4 albedo : hint_color = vec4(1, 1, 1, 1);
uniform sampler2D texture_albedo : hint_albedo;
uniform float specular = 0.5;
uniform float metallic = 0;
uniform float roughness : hint_range(0,1) = 1;
uniform float point_size : hint_range(0,128) = 1;
uniform vec3 uv1_scale = vec3(1);
uniform vec3 uv1_offset = vec3(0);
uniform vec3 uv2_scale = vec3(1);
uniform vec3 uv2_offset = vec3(0);
uniform mat4 clipBoxInverseTransform;

float PointVsBox(vec3 worldPosition, mat4 boxInverseTransform)
{
   vec3 distance = abs(boxInverseTransform * vec4(worldPosition, 1.0)).xyz;
   return 1.0 - step(1.0001, max(distance.x, max(distance.y, distance.z)));
}

void vertex() {
	UV=UV*uv1_scale.xy+uv1_offset.xy;
}




void fragment() {
	vec2 base_uv = UV;
	vec4 albedo_tex = texture(texture_albedo,base_uv);
	ALBEDO = albedo.rgb * albedo_tex.rgb;
	METALLIC = metallic;
	ROUGHNESS = roughness;
	SPECULAR = specular;
	vec3 global_vertex = (CAMERA_MATRIX * vec4(VERTEX, 1.0)).xyz;
	ALPHA_SCISSOR = 1.0;
	ALPHA = albedo.a * albedo_tex.a * PointVsBox(global_vertex, clipBoxInverseTransform);
}
