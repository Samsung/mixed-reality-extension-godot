// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.GodotHelper;

namespace MixedRealityExtension.Core.Components
{
	internal class ClippingComponent : ActorComponentBase
	{
		private const string DefaultClippingShaderCode =
		"shader_type spatial;\n" +
		ClippingFunctionShaderCode +
		"void fragment() {\n" +
			AlphaSetterShaderCode +
		"}";

		private const string ClippingFunctionShaderCode =
		"uniform mat4 clipBoxInverseTransform;\n" +
		"float PointVsBox(vec3 worldPosition, mat4 boxInverseTransform)\n" +
		"{\n" +
		"   vec3 distance = abs(boxInverseTransform * vec4(worldPosition, 1.0)).xyz;\n" +
		"   return 1.0 - step(1.0001, max(distance.x, max(distance.y, distance.z)));\n" +
		"}\n";

		private const string AlphaSetterShaderCode =
		"   vec3 gv = (CAMERA_MATRIX * vec4(VERTEX, 1.0)).xyz;\n" +
		"   ALPHA *= PointVsBox(gv, clipBoxInverseTransform);\n";

		private List<MeshInstance> meshInstances = new List<MeshInstance>();
		private Dictionary<MeshInstance, Material> originMaterials = new Dictionary<MeshInstance, Material>();
		private static Vector3 Vector3Half = Vector3.One * 0.5f;

		internal void ApplyPatch(ClippingPatch patch)
		{
			ClearMeshInstances();
			if (patch.ClippingObjects != null)
			{
				foreach (var clippingObjectId in patch.ClippingObjects)
				{
					Node targetActor = AttachedActor.App.FindActor(clippingObjectId) as Node;
					MWGOTreeWalker.VisitTree(targetActor, node =>
					{
						if (node is MeshInstance meshInstance)
						{
							AddMeshInstance(meshInstance);
						}
					});
				}
			}
		}

		public override void _Process(float delta)
		{
			var globalTransform = GlobalTransform;
			globalTransform.basis = globalTransform.basis.Scaled(Vector3Half);
			foreach (var meshInstance in meshInstances)
			{
				if (meshInstance.IsVisibleInTree())
					((ShaderMaterial)(meshInstance.MaterialOverride)).SetShaderParam("clipBoxInverseTransform", globalTransform.AffineInverse());
			}
		}

		public void AddMeshInstance(MeshInstance meshInstance)
		{
			if (meshInstance != null)
			{
				if (!meshInstances.Contains(meshInstance))
				{
					if (meshInstance.MaterialOverride != null)
					{
						originMaterials[meshInstance] = meshInstance.MaterialOverride;
						SetClippingMaterial(meshInstance, meshInstance.MaterialOverride);
						meshInstances.Add(meshInstance);
						return;
					}
					if (meshInstance.Mesh != null)
					{
						var materialCount = meshInstance.Mesh.GetSurfaceCount();
						for (int i = 0; i < materialCount; i++)
						{
							var material = meshInstance.Mesh.SurfaceGetMaterial(i);
							if (material != null)
								SetClippingMaterial(meshInstance, material);
						}
					}
					else
					{
						var materialCount = meshInstance.GetSurfaceMaterialCount();
						for (int i = 0; i < materialCount; i++)
						{
							var material = meshInstance.GetSurfaceMaterial(i);
							if (material != null)
								SetClippingMaterial(meshInstance, material);
						}
					}

					if (meshInstance.MaterialOverride == null)
						SetClippingMaterial(meshInstance);
					meshInstances.Add(meshInstance);
				}
			}
		}

		public void RemoveMeshInstance(MeshInstance meshInstance)
		{
			if (originMaterials.Remove(meshInstance, out Material material))
			{
				meshInstance.MaterialOverride = material;
			}
			else
			{
				meshInstance.MaterialOverride = null;
			}
			meshInstances.Remove(meshInstance);
		}

		public void ClearMeshInstances()
		{
			while (meshInstances.Count > 0)
			{
				RemoveMeshInstance(meshInstances.Last());
			}

			foreach (var meshMaterialPair in originMaterials)
			{
				var meshInstance = meshMaterialPair.Key;
				var material = meshMaterialPair.Value;

				meshInstance.MaterialOverride = material;
			}
			originMaterials.Clear();
		}

		private async void SetClippingMaterial(MeshInstance meshInstance, Material originMaterial = null)
		{
			var clippingMaterial = new ShaderMaterial();
			clippingMaterial.Shader = new Shader();

			if (originMaterial is ShaderMaterial shaderMaterial)
			{
				var shader = shaderMaterial.Shader;
				if (shader != null)
				{
					clippingMaterial.Shader.Code = InsertClippingFunction(shader.Code);
					clippingMaterial.SetShaderParam("color", shaderMaterial.GetShaderParam("color"));
					clippingMaterial.SetShaderParam("origin", shaderMaterial.GetShaderParam("origin"));
					clippingMaterial.SetShaderParam("backward", shaderMaterial.GetShaderParam("backward"));
				}
			}
			else if (originMaterial is SpatialMaterial spatialMaterial)
			{
				string shaderCode =  await GetShaderCodeFromSpatialMaterial(spatialMaterial);
				clippingMaterial.Shader.Code = InsertClippingFunction(shaderCode);
				clippingMaterial.SetShaderParam("albedo", spatialMaterial.AlbedoColor);
				clippingMaterial.SetShaderParam("specular", spatialMaterial.MetallicSpecular);
				clippingMaterial.SetShaderParam("roughness", spatialMaterial.Roughness);
				clippingMaterial.SetShaderParam("metallic", spatialMaterial.Metallic);
				clippingMaterial.SetShaderParam("emission", spatialMaterial.Emission);
				clippingMaterial.SetShaderParam("emission_energy", spatialMaterial.EmissionEnergy);
				clippingMaterial.SetShaderParam("normal_scale", spatialMaterial.NormalScale);
				clippingMaterial.SetShaderParam("rim", spatialMaterial.Rim);
				clippingMaterial.SetShaderParam("rim_tint", spatialMaterial.RimTint);
				clippingMaterial.SetShaderParam("clearcoat", spatialMaterial.Clearcoat);
				clippingMaterial.SetShaderParam("clearcoat_gloss", spatialMaterial.ClearcoatGloss);
				clippingMaterial.SetShaderParam("anisotropy_ratio", spatialMaterial.Anisotropy);
				clippingMaterial.SetShaderParam("depth_scale", spatialMaterial.DepthScale);
				clippingMaterial.SetShaderParam("subsurface_scattering_strength", spatialMaterial.SubsurfScatterStrength);
				clippingMaterial.SetShaderParam("transmission", spatialMaterial.Transmission);
				clippingMaterial.SetShaderParam("refraction", spatialMaterial.RefractionScale);
				clippingMaterial.SetShaderParam("point_size", spatialMaterial.ParamsPointSize);
				clippingMaterial.SetShaderParam("uv1_scale", spatialMaterial.Uv1Scale);
				clippingMaterial.SetShaderParam("uv1_offset", spatialMaterial.Uv1Offset);
				clippingMaterial.SetShaderParam("uv2_scale", spatialMaterial.Uv2Scale);
				clippingMaterial.SetShaderParam("uv2_offset", spatialMaterial.Uv2Offset);
				clippingMaterial.SetShaderParam("uv1_blend_sharpness", spatialMaterial.Uv1TriplanarSharpness);
				clippingMaterial.SetShaderParam("uv2_blend_sharpness", spatialMaterial.Uv2TriplanarSharpness);

				clippingMaterial.SetShaderParam("particles_anim_h_frames", spatialMaterial.ParticlesAnimHFrames);
				clippingMaterial.SetShaderParam("particles_anim_v_frames", spatialMaterial.ParticlesAnimVFrames);
				clippingMaterial.SetShaderParam("particles_anim_loop", spatialMaterial.ParticlesAnimLoop);
				clippingMaterial.SetShaderParam("depth_min_layers", spatialMaterial.DepthMinLayers);
				clippingMaterial.SetShaderParam("depth_max_layers", spatialMaterial.DepthMaxLayers);
				clippingMaterial.SetShaderParam("depth_flip", new Vector2(spatialMaterial.DepthFlipTangent ? -1 : 1, spatialMaterial.DepthFlipBinormal ? -1 : 1));

				clippingMaterial.SetShaderParam("grow", spatialMaterial.ParamsGrow);

				clippingMaterial.SetShaderParam("ao_light_affect", spatialMaterial.AoLightAffect);

				clippingMaterial.SetShaderParam("proximity_fade_distance", spatialMaterial.ProximityFadeDistance);
				clippingMaterial.SetShaderParam("distance_fade_min", spatialMaterial.DistanceFadeMinDistance);
				clippingMaterial.SetShaderParam("distance_fade_max", spatialMaterial.DistanceFadeMaxDistance);

				clippingMaterial.SetShaderParam("metallic_texture_channel", spatialMaterial.MetallicTextureChannel);
				clippingMaterial.SetShaderParam("roughness_texture_channel", spatialMaterial.RoughnessTextureChannel);
				clippingMaterial.SetShaderParam("ao_texture_channel", spatialMaterial.AoTextureChannel);
				clippingMaterial.SetShaderParam("refraction_texture_channel", spatialMaterial.RefractionTextureChannel);
				clippingMaterial.SetShaderParam("alpha_scissor_threshold", spatialMaterial.ParamsAlphaScissorThreshold);

				clippingMaterial.SetShaderParam("texture_albedo", spatialMaterial.AlbedoTexture);
				clippingMaterial.SetShaderParam("texture_metallic", spatialMaterial.MetallicTexture);
				clippingMaterial.SetShaderParam("texture_roughness", spatialMaterial.RoughnessTexture);
				clippingMaterial.SetShaderParam("texture_emission", spatialMaterial.EmissionTexture);
				clippingMaterial.SetShaderParam("texture_normal", spatialMaterial.NormalTexture);
				clippingMaterial.SetShaderParam("texture_rim", spatialMaterial.RimTexture);
				clippingMaterial.SetShaderParam("texture_clearcoat", spatialMaterial.ClearcoatTexture);
				clippingMaterial.SetShaderParam("texture_flowmap", spatialMaterial.AnisotropyFlowmap);
				clippingMaterial.SetShaderParam("texture_ambient_occlusion", spatialMaterial.AoTexture);
				clippingMaterial.SetShaderParam("texture_depth", spatialMaterial.DepthTexture);
				clippingMaterial.SetShaderParam("texture_subsurface_scattering", spatialMaterial.SubsurfScatterTexture);
				clippingMaterial.SetShaderParam("texture_transmission", spatialMaterial.TransmissionTexture);
				clippingMaterial.SetShaderParam("texture_refraction", spatialMaterial.RefractionTexture);
				clippingMaterial.SetShaderParam("texture_detail_mask", spatialMaterial.DetailMask);
				clippingMaterial.SetShaderParam("texture_detail_albedo", spatialMaterial.DetailAlbedo);
				clippingMaterial.SetShaderParam("texture_detail_normal", spatialMaterial.DetailNormal);
			}

			if (String.IsNullOrEmpty(clippingMaterial.Shader.Code))
				clippingMaterial.Shader.Code = DefaultClippingShaderCode;
			meshInstance.MaterialOverride = clippingMaterial;
		}

		private async Task<string> GetShaderCodeFromSpatialMaterial(SpatialMaterial spatialMaterial)
		{
			var shaderRID = VisualServer.MaterialGetShader(spatialMaterial.GetRid());
			string shaderCode = VisualServer.ShaderGetCode(shaderRID);
			while (string.IsNullOrEmpty(shaderCode))
			{
				await ToSignal(GetTree().CreateTimer(0.016f), "timeout");
				shaderRID = VisualServer.MaterialGetShader(spatialMaterial.GetRid());
				shaderCode = VisualServer.ShaderGetCode(shaderRID);
			}
			return shaderCode;
		}

		private string InsertClippingFunction(string shaderCode)
		{
			var origin = shaderCode;
			string newCode;
			if (origin.Contains("clipBoxInverseTransform"))
				return origin;
			var fragmentIndex = shaderCode.IndexOf("void fragment(");
			if (fragmentIndex < 0)
			{
				newCode = origin +
					ClippingFunctionShaderCode +
					"void fragment() {\n" +
						AlphaSetterShaderCode +
					"}";
			}
			else
			{
				newCode = origin.Substring(0, fragmentIndex - 1);
				newCode += "\n" + ClippingFunctionShaderCode;

				var fragmentCode = origin.Substring(fragmentIndex);
				var match = Regex.Match(fragmentCode, "(?<=\\{)[^}]*(?=\\})");
				var restCode = fragmentCode.Substring(match.Index + match.Value.Length);
				fragmentCode = fragmentCode.Substring(0, match.Index + match.Value.Length);
				fragmentCode += "\n" + AlphaSetterShaderCode;
				newCode += fragmentCode + restCode;
			}

			return newCode;
		}
	}
}