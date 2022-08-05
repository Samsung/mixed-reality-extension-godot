// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using Godot;

namespace MixedRealityExtension.Core
{
	internal class ClippingBox : ClippingBase
	{
		private static Vector3 Vector3Half = Vector3.One * 0.5f;

		public override void _Process(float delta)
		{
			var globalTransform = GlobalTransform;
			globalTransform.basis = globalTransform.basis.Scaled(Vector3Half);
			var affineInverse = globalTransform.AffineInverse();
			foreach (var shaderMaterial in ShaderMaterials())
			{
				shaderMaterial.SetShaderParam("clipBoxInverseTransform", affineInverse);
			}
		}

		public override void ClearMeshInstance3Ds()
		{
			base.ClearMeshInstance3Ds();
			foreach (var shaderMaterial in ShaderMaterials())
			{
				// clear inverse transform matrix
				shaderMaterial.SetShaderParam("clipBoxInverseTransform", null);
			}
		}
	}
}