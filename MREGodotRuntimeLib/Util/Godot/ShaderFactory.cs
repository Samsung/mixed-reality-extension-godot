using Godot;

namespace MixedRealityExtension.Util.GodotHelper
{
	public static class ShaderFactory
	{
		public static Shader OpaqueShader = new Shader() { Code = ShaderFactory.Get(ShaderFactory.ShaderType.Opaque) };
		public static Shader BlendShader = new Shader() { Code = ShaderFactory.Get(ShaderFactory.ShaderType.Blend) };
		public static Shader MaskShader = new Shader() { Code = ShaderFactory.Get(ShaderFactory.ShaderType.Mask) };

		private enum ShaderType
		{
			Mask,
			Blend,
			Opaque
		}

		private static string Get(ShaderType shaderType)
		{
			var shader =
				"shader_type spatial;\n" +
				"render_mode blend_mix,depth_draw_opaque,cull_back,diffuse_burley,specular_schlick_ggx,depth_prepass_alpha;\n" +
				"uniform vec4 albedo : source_color = vec4(1, 1, 1, 1);\n" +
				"uniform sampler2D texture_albedo : source_color,filter_linear_mipmap,repeat_enable;\n" +
				"uniform float specular = 0.5;\n" +
				"uniform float metallic;\n" +
				"uniform float roughness : hint_range(0,1) = 1.0;\n" +
				"uniform float point_size : hint_range(0,128) = 1.0;\n" +
				"uniform sampler2D texture_emission : source_color, hint_default_black,filter_linear_mipmap,repeat_enable;\n" +
				"uniform vec4 emission : source_color;\n" +
				"uniform float emission_energy = 1.0;\n" +
				"uniform vec3 uv1_scale = vec3(1);\n" +
				"uniform vec3 uv1_offset = vec3(0);\n" +
				"uniform vec3 uv2_scale = vec3(1);\n" +
				"uniform vec3 uv2_offset = vec3(0);\n" +
				"uniform mat4 clipBoxInverseTransform;\n";
			if (shaderType == ShaderType.Mask)
				shader += "uniform float alpha_scissor_threshold = 1.0;\n";
			shader +=
			"float PointVsBox(vec3 worldPosition, mat4 boxInverseTransform)\n" +
			"{\n" +
			"	vec3 distance = abs(boxInverseTransform * vec4(worldPosition, 1.0)).xyz;\n" +
			"	return 1.0 - step(1.0001, max(distance.x, max(distance.y, distance.z)));\n" +
			"}\n\n" +

			"void vertex() {\n" +
			"	UV=UV*uv1_scale.xy+uv1_offset.xy;\n" +
			"	UV2=UV2*uv2_scale.xy+uv2_offset.xy;\n" +
			"}\n" +

			"void fragment() {\n" +
			"	vec2 base_uv = UV;\n" +
			"	vec2 base_uv2 = UV2;\n" +
			"	vec4 albedo_tex = texture(texture_albedo,base_uv);\n" +
			"	albedo_tex *= COLOR;\n" +
			"	ALBEDO = albedo.rgb * albedo_tex.rgb;\n" +
			"	METALLIC = metallic;\n" +
			"	ROUGHNESS = roughness;\n" +
			"	SPECULAR = specular;\n" +
			"	vec3 emission_tex = texture(texture_emission,base_uv2).rgb;\n" +
			"	EMISSION = (emission.rgb+emission_tex)*emission_energy;\n" +

			"	vec3 global_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;\n";
			if (shaderType != ShaderType.Opaque)
			{
				shader += "	ALPHA *= albedo.a * albedo_tex.a * PointVsBox(global_vertex, clipBoxInverseTransform);\n";
			}
			shader += "	if (PointVsBox(global_vertex, clipBoxInverseTransform) <= 0.0) discard;\n";

			if (shaderType == ShaderType.Mask)
				shader += "ALPHA_SCISSOR_THRESHOLD = 1.0;\n";

			shader +="}\n";

			return shader;
		}
	}
}