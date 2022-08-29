shader_type spatial;
render_mode blend_mix,depth_draw_opaque,cull_disabled,diffuse_burley,specular_schlick_ggx,depth_prepass_alpha,unshaded;
uniform vec4 albedo : source_color=vec4(1);
uniform sampler2D texture_albedo : source_color,filter_linear_mipmap,repeat_enable;
uniform vec3 uv1_scale=vec3(1);
uniform vec3 uv1_offset=vec3(0);
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
	vec3 global_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	//ALPHA_SCISSOR_THRESHOLD = 1.0;
	ALPHA *= albedo.a * albedo_tex.a; //* PointVsBox(global_vertex, clipBoxInverseTransform);
	if (PointVsBox(global_vertex, clipBoxInverseTransform) <= 0.0) discard;
}
