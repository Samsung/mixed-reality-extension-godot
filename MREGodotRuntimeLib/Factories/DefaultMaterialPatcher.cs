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
