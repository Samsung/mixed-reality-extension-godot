// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Util.GodotHelper;
using System;
using System.Collections.Generic;
using MWAssets = MixedRealityExtension.Assets;
using Material = Godot.ShaderMaterial;
using Texture = Godot.Texture;
using Godot;

namespace MixedRealityExtension.Factories
{
	/// <summary>
	/// Default implementation of IMaterialPatcher. Only handles color and mainTexture property updates.
	/// </summary>
	public class DefaultMaterialPatcher : IMaterialPatcher
	{
		protected Dictionary<ulong, Guid> mainTextureAssignments = new Dictionary<ulong, Guid>(20);
		protected Dictionary<ulong, Guid> emissiveTextureAssignments = new Dictionary<ulong, Guid>(20);
		private MWColor _materialColor = new MWColor();
		private MWVector3 _textureOffset = new MWVector3();
		private MWVector3 _textureScale = new MWVector3();

		private static string AlbedoColorProp = "albedo";
		private static string EmissionColorProp = "emission";
		private static string Uv1OffsetProp = "uv1_offset";
		private static string Uv1ScaleProp = "uv1_scale";
		private static string Uv2OffsetProp = "uv2_offset";
		private static string Uv2ScaleProp = "uv2_scale";
		private static string TextureAlbedoProp = "texture_albedo";
		private static string TextureEmissionProp = "texture_emission";
		private static string AlphaScissorThresholdProp = "alpha_scissor_threshold";

		private static Shader OpaqueShader = ResourceLoader.Load<Shader>("res://MREGodotRuntimeLib/Shaders/MREDefaultShader_Opaque.shader");
		private static Shader BlendShader = ResourceLoader.Load<Shader>("res://MREGodotRuntimeLib/Shaders/MREDefaultShader_Blend.shader");
		private static Shader MaskShader = ResourceLoader.Load<Shader>("res://MREGodotRuntimeLib/Shaders/MREDefaultShader_Mask.shader");


		/// <inheritdoc />
		public virtual void ApplyMaterialPatch(IMixedRealityExtensionApp app, Material material, MaterialPatch patch)
		{
			switch (patch.AlphaMode)
			{
				case MWAssets.AlphaMode.Opaque:
					material.Shader = OpaqueShader;
					break;
				case MWAssets.AlphaMode.Mask:
					material.Shader = MaskShader;
					break;
				case MWAssets.AlphaMode.Blend:
					material.Shader = BlendShader;
					break;
				// ignore default case, i.e. null
			}
			if (patch.Color != null)
			{
				_materialColor.FromGodotColor((Color)material.GetShaderParam(AlbedoColorProp));
				_materialColor.ApplyPatch(patch.Color);
				material.SetShaderParam(AlbedoColorProp, _materialColor.ToColor());
			}

			if (patch.MainTextureOffset != null)
			{
				_textureOffset.FromGodotVector3((Vector3)material.GetShaderParam(Uv1OffsetProp));
				_textureOffset.ApplyPatch(patch.MainTextureOffset);
				_textureOffset.Y *= -1;
				material.SetShaderParam(Uv1OffsetProp, _textureOffset.ToVector3());
			}

			if (patch.MainTextureScale != null)
			{
				_textureScale.FromGodotVector3((Vector3)material.GetShaderParam(Uv1ScaleProp));
				_textureScale.ApplyPatch(patch.MainTextureScale);
				material.SetShaderParam(Uv1ScaleProp, _textureScale.ToVector3());
			}

			if (patch.MainTextureId != null)
			{
				var textureId = patch.MainTextureId.Value;
				mainTextureAssignments[material.GetInstanceId()] = textureId;
				if (patch.MainTextureId == Guid.Empty)
				{
					material.SetShaderParam(TextureAlbedoProp, material.PropertyGetRevert(TextureAlbedoProp));
				}
				else
				{
					app.AssetManager.OnSet(textureId, tex =>
					{
						if (material == null || mainTextureAssignments[material.GetInstanceId()] != textureId) return;
						material.SetShaderParam(TextureAlbedoProp, tex.Asset);
					});
				}
			}

			if (patch.EmissiveColor != null)
			{
				var color = (Color)material.GetShaderParam(EmissionColorProp);
				color.r = patch.EmissiveColor.R ?? color.r;
				color.g = patch.EmissiveColor.G ?? color.g;
				color.b = patch.EmissiveColor.B ?? color.b;
				color.a = patch.EmissiveColor.A ?? color.a;
				material.SetShaderParam(EmissionColorProp, color);
			}

			if (patch.EmissiveTextureOffset != null)
			{
				var offset = (Vector3)material.GetShaderParam(Uv2OffsetProp);
				offset.x = patch.EmissiveTextureOffset.X ?? offset.x;
				offset.y = patch.EmissiveTextureOffset.Y ?? offset.y;
				material.SetShaderParam(Uv2OffsetProp, offset);
			}

			if (patch.EmissiveTextureScale != null)
			{
				var scale = (Vector3)material.GetShaderParam(Uv2ScaleProp);
				scale.x = patch.EmissiveTextureScale.X ?? scale.x;
				scale.y = patch.EmissiveTextureScale.Y ?? scale.y;
				material.SetShaderParam(Uv2ScaleProp, scale);
			}

			if (patch.EmissiveTextureId != null)
			{
				var textureId = patch.EmissiveTextureId.Value;
				emissiveTextureAssignments[material.GetInstanceId()] = textureId;
				if (textureId == Guid.Empty)
				{
					material.SetShaderParam(TextureEmissionProp, material.PropertyGetRevert(TextureEmissionProp));
				}
				else
				{
					app.AssetManager.OnSet(textureId, tex =>
					{
						if (material == null || emissiveTextureAssignments[material.GetInstanceId()] != textureId) return;
						material.SetShaderParam(TextureEmissionProp, tex.Asset);
					});
				}
			}

			if (patch.AlphaCutoff != null)
			{
				material.SetShaderParam(AlphaScissorThresholdProp, patch.AlphaCutoff.Value);
			}
		}

		/// <inheritdoc />
		public virtual MaterialPatch GeneratePatch(IMixedRealityExtensionApp app, Material material)
		{
			return new MaterialPatch()
			{
				Color = material.GetShaderParam(AlbedoColorProp) is Color albedoColor ? new ColorPatch(albedoColor) : null,
				MainTextureId = material.GetShaderParam(TextureAlbedoProp) is Godot.Object textureAlbedo ? app.AssetManager.GetByObject(textureAlbedo)?.Id : null,
				MainTextureOffset = material.GetShaderParam(Uv1OffsetProp) is Vector3 uv1Offset ? new Vector3Patch(uv1Offset) : null,
				MainTextureScale = material.GetShaderParam(Uv1ScaleProp) is Vector3 uv1Scale ? new Vector3Patch(uv1Scale) : null,
				EmissiveColor = material.GetShaderParam(EmissionColorProp) is Color emissionColor ? new ColorPatch(emissionColor): null,
				EmissiveTextureId = material.GetShaderParam(TextureEmissionProp) is Godot.Object textureEmission ? app.AssetManager.GetByObject(textureEmission)?.Id : null,
				EmissiveTextureOffset = material.GetShaderParam(Uv2OffsetProp) is Vector3 uv2Offset ? new Vector2Patch(ToVector2(uv2Offset)) : null,
				EmissiveTextureScale = material.GetShaderParam(Uv2ScaleProp) is Vector3 uv2Scale ? new Vector2Patch(ToVector2(uv2Scale)) : null
			};
		}

		/// <inheritdoc />
		public virtual bool UsesTexture(IMixedRealityExtensionApp app, Material material, Texture texture)
		{
			return material.GetShaderParam(TextureAlbedoProp) == texture;
		}

		private static Vector2 ToVector2(Vector3 vec3)
		{
			return new Vector2(vec3.x, vec3.y);
		}
	}
}
