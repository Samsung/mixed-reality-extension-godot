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
using Material = Godot.SpatialMaterial;
using MWMaterial = MixedRealityExtension.Assets.Material;
using Texture = Godot.Texture;

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

		/// <inheritdoc />
		public virtual void ApplyMaterialPatch(IMixedRealityExtensionApp app, Material material, MWMaterial patch)
		{
			if (patch.Color != null)
			{
				_materialColor.FromGodotColor(material.AlbedoColor);
				_materialColor.ApplyPatch(patch.Color);
				material.AlbedoColor = _materialColor.ToColor();
			}

			if (patch.MainTextureOffset != null)
			{
				_textureOffset.FromGodotVector3(material.Uv1Offset);
				_textureOffset.ApplyPatch(patch.MainTextureOffset);
				_textureOffset.Y *= -1;
				material.Uv1Offset = _textureOffset.ToVector3();
			}

			if (patch.MainTextureScale != null)
			{
				_textureScale.FromGodotVector3(material.Uv1Scale);
				_textureScale.ApplyPatch(patch.MainTextureScale);
				material.Uv1Scale = _textureScale.ToVector3();
			}

			if (patch.MainTextureId != null)
			{
				var textureId = patch.MainTextureId.Value;
				mainTextureAssignments[material.GetInstanceId()] = textureId;
				if (patch.MainTextureId == Guid.Empty)
				{
					material.AlbedoTexture = null;
				}
				else
				{
					app.AssetManager.OnSet(textureId, tex =>
					{
						if (material == null || mainTextureAssignments[material.GetInstanceId()] != textureId) return;
						material.AlbedoTexture = (Texture)tex.Asset;
					});
				}
			}

			if (patch.EmissiveColor != null)
			{
				material.EmissionEnabled = true;
				var color = material.Emission;
				color.r = patch.EmissiveColor.R ?? color.r;
				color.g = patch.EmissiveColor.G ?? color.g;
				color.b = patch.EmissiveColor.B ?? color.b;
				color.a = patch.EmissiveColor.A ?? color.a;
				material.Emission = color;
			}

			if (patch.EmissiveTextureOffset != null)
			{
				material.EmissionEnabled = true;
				var offset = material.Uv2Offset;
				offset.x = patch.EmissiveTextureOffset.X ?? offset.x;
				offset.y = patch.EmissiveTextureOffset.Y ?? offset.y;
				material.Uv2Offset = offset;
			}

			if (patch.EmissiveTextureScale != null)
			{
				material.EmissionEnabled = true;
				var scale = material.Uv2Scale;
				scale.x = patch.EmissiveTextureScale.X ?? scale.x;
				scale.y = patch.EmissiveTextureScale.Y ?? scale.y;
				material.Uv2Scale = scale;
			}

			if (patch.EmissiveTextureId != null)
			{
				material.EmissionEnabled = true;
				var textureId = patch.EmissiveTextureId.Value;
				emissiveTextureAssignments[material.GetInstanceId()] = textureId;
				if (textureId == Guid.Empty)
				{
					material.EmissionTexture = null;
				}
				else
				{
					app.AssetManager.OnSet(textureId, tex =>
					{
						if (material == null || emissiveTextureAssignments[material.GetInstanceId()] != textureId) return;
						material.EmissionTexture = (Texture)tex.Asset;
					});
				}
			}

			if (patch.AlphaCutoff != null)
			{
				material.ParamsUseAlphaScissor = true;
				material.ParamsAlphaScissorThreshold = patch.AlphaCutoff.Value;
			}

			switch (patch.AlphaMode)
			{
				case MWAssets.AlphaMode.Opaque:
					material.FlagsTransparent = false;
					break;
				case MWAssets.AlphaMode.Mask:
					material.ParamsUseAlphaScissor = true;
					material.ParamsAlphaScissorThreshold = 1.0f;
					break;
				case MWAssets.AlphaMode.Blend:
					material.FlagsTransparent = true;
					material.ParamsUseAlphaScissor = false;
					break;
				// ignore default case, i.e. null
			}

		}

		/// <inheritdoc />
		public virtual MWMaterial GeneratePatch(IMixedRealityExtensionApp app, Material material)
		{
			return new MWMaterial()
			{
				Color = new ColorPatch(material.AlbedoColor),
				MainTextureId = app.AssetManager.GetByObject(material.AlbedoTexture)?.Id,
				MainTextureOffset = new Vector3Patch(material.Uv1Offset),
				MainTextureScale = new Vector3Patch(material.Uv1Scale)
			};
		}

		/// <inheritdoc />
		public virtual bool UsesTexture(IMixedRealityExtensionApp app, Material material, Texture texture)
		{
			return material.AlbedoTexture == texture;
		}
	}
}
