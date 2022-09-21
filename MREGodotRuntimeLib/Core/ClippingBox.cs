// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Godot;

namespace MixedRealityExtension.Core
{
	internal partial class ClippingBox : ClippingBase
	{
		private static Vector3 Vector3Half = Vector3.One * 0.5f;

		public override void _Process(double delta)
		{
			var globalTransform = GlobalTransform;
			globalTransform.basis = globalTransform.basis.Scaled(Vector3Half);
			var affineInverse = globalTransform.AffineInverse();
			foreach (var shaderMaterial in ShaderMaterialRIDs())
			{
				RenderingServer.MaterialSetParam(shaderMaterial, "clipBoxInverseTransform", affineInverse);
			}
			foreach (MeshInstance3D meshInstance in GetNodesCopy().Where(node => node is MeshInstance3D))
			{
				var count = meshInstance.GetSurfaceOverrideMaterialCount();
				for (int i = 0; i < count; i++) {
					var shaderMaterial = meshInstance.GetSurfaceOverrideMaterial(i) as ShaderMaterial;
					shaderMaterial.SetShaderParameter( "clipBoxInverseTransform", affineInverse);
				}
			}
		}

		public override void ClearMeshInstances()
		{
			base.ClearMeshInstances();
			foreach (var shaderMaterial in ShaderMaterialRIDs())
			{
				// clear inverse transform matrix
				RenderingServer.MaterialSetParam(shaderMaterial, "clipBoxInverseTransform", new Variant());
			}
		}
	}
}