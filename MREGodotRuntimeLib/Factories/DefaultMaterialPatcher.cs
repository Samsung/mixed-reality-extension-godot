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
using Texture = Godot.Texture2D;
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

		/// <inheritdoc />
		public virtual void ApplyMaterialPatch(IMixedRealityExtensionApp app, Material material, MaterialPatch patch)
		{
			switch (patch.AlphaMode)
			{
				case MWAssets.AlphaMode.Opaque:
					material.Shader = ShaderFactory.OpaqueShader;
					break;
				case MWAssets.AlphaMode.Mask:
					material.Shader = ShaderFactory.MaskShader;
					break;
				case MWAssets.AlphaMode.Blend:
					material.Shader = ShaderFactory.BlendShader;
					break;
				// ignore default case, i.e. null
			}
			if (patch.Color != null)
			{
				var color = material.GetShaderParameter(AlbedoColorProp).AsColor();
				_materialColor.FromGodotColor(color);
				_materialColor.ApplyPatch(patch.Color);
				material.SetShaderParameter(AlbedoColorProp, _materialColor.ToColor());
			}

			if (patch.MainTextureOffset != null)
			{
				var uv1Offset = material.GetShaderParameter(Uv1OffsetProp).AsVector3();
				_textureOffset.FromGodotVector3(uv1Offset);
				_textureOffset.ApplyPatch(patch.MainTextureOffset);
				_textureOffset.Y *= -1;
				material.SetShaderParameter(Uv1OffsetProp, _textureOffset.ToVector3());
			}

			if (patch.MainTextureScale != null)
			{
				var uv1Scale = material.GetShaderParameter(Uv1ScaleProp).AsVector3();
				_textureScale.FromGodotVector3(uv1Scale);
				_textureScale.ApplyPatch(patch.MainTextureScale);
				material.SetShaderParameter(Uv1ScaleProp, _textureScale.ToVector3());
			}

			if (patch.MainTextureId != null)
			{
				var textureId = patch.MainTextureId.Value;
				mainTextureAssignments[material.GetInstanceId()] = textureId;
				if (patch.MainTextureId == Guid.Empty)
				{
					material.SetShaderParameter(TextureAlbedoProp, material._PropertyGetRevert(TextureAlbedoProp));
				}
				else
				{
					app.AssetManager.OnSet(textureId, tex =>
					{
						if (material == null || mainTextureAssignments[material.GetInstanceId()] != textureId) return;
						material.SetShaderParameter(TextureAlbedoProp, tex.Asset);
					});
				}
			}

			if (patch.EmissiveColor != null)
			{
				var color = material.GetShaderParameter(EmissionColorProp).AsColor();
				color.r = patch.EmissiveColor.R ?? color.r;
				color.g = patch.EmissiveColor.G ?? color.g;
				color.b = patch.EmissiveColor.B ?? color.b;
				color.a = patch.EmissiveColor.A ?? color.a;
				material.SetShaderParameter(EmissionColorProp, color);
			}

			if (patch.EmissiveTextureOffset != null)
			{
				var offset = material.GetShaderParameter(Uv2OffsetProp).AsVector3();
				offset.x = patch.EmissiveTextureOffset.X ?? offset.x;
				offset.y = patch.EmissiveTextureOffset.Y ?? offset.y;
				material.SetShaderParameter(Uv2OffsetProp, offset);
			}

			if (patch.EmissiveTextureScale != null)
			{
				var scale = material.GetShaderParameter(Uv2ScaleProp).AsVector3();
				scale.x = patch.EmissiveTextureScale.X ?? scale.x;
				scale.y = patch.EmissiveTextureScale.Y ?? scale.y;
				material.SetShaderParameter(Uv2ScaleProp, scale);
			}

			if (patch.EmissiveTextureId != null)
			{
				var textureId = patch.EmissiveTextureId.Value;
				emissiveTextureAssignments[material.GetInstanceId()] = textureId;
				if (textureId == Guid.Empty)
				{
					material.SetShaderParameter(TextureEmissionProp, material._PropertyGetRevert(TextureEmissionProp));
				}
				else
				{
					app.AssetManager.OnSet(textureId, tex =>
					{
						if (material == null || emissiveTextureAssignments[material.GetInstanceId()] != textureId) return;
						material.SetShaderParameter(TextureEmissionProp, tex.Asset);
					});
				}
			}

			if (patch.AlphaCutoff != null)
			{
				material.SetShaderParameter(AlphaScissorThresholdProp, patch.AlphaCutoff.Value);
			}
		}

		/// <inheritdoc />
		public virtual MaterialPatch GeneratePatch(IMixedRealityExtensionApp app, Material material)
		{
			return new MaterialPatch()
			{
				Color = material.GetShaderParameter(AlbedoColorProp).Obj is Color albedoColor ? new ColorPatch(albedoColor) : null,
				MainTextureId = material.GetShaderParameter(TextureAlbedoProp).Obj is Godot.Object textureAlbedo ? app.AssetManager.GetByObject(textureAlbedo)?.Id : null,
				MainTextureOffset = material.GetShaderParameter(Uv1OffsetProp).Obj is Vector3 uv1Offset ? new Vector3Patch(uv1Offset) : null,
				MainTextureScale = material.GetShaderParameter(Uv1ScaleProp).Obj is Vector3 uv1Scale ? new Vector3Patch(uv1Scale) : null,
				EmissiveColor = material.GetShaderParameter(EmissionColorProp).Obj is Color emissionColor ? new ColorPatch(emissionColor): null,
				EmissiveTextureId = material.GetShaderParameter(TextureEmissionProp).Obj is Godot.Object textureEmission ? app.AssetManager.GetByObject(textureEmission)?.Id : null,
				EmissiveTextureOffset = material.GetShaderParameter(Uv2OffsetProp).Obj is Vector3 uv2Offset ? new Vector2Patch(ToVector2(uv2Offset)) : null,
				EmissiveTextureScale = material.GetShaderParameter(Uv2ScaleProp).Obj is Vector3 uv2Scale ? new Vector2Patch(ToVector2(uv2Scale)) : null
			};
		}

		private static Vector2 ToVector2(Vector3 vec3)
		{
			return new Vector2(vec3.x, vec3.y);
		}
	}
}
