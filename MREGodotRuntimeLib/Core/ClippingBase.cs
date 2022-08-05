// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Godot;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.GodotHelper;

namespace MixedRealityExtension.Core
{
	public abstract partial class ClippingBase : Spatial
	{
		private List<Spatial> meshInstances = new List<Spatial>();
		private IActor parent;

		public AABB Bounds => new AABB(GlobalTransform.origin - GlobalTransform.basis.Scale / 2, GlobalTransform.basis.Scale);

		internal void ApplyPatch(ClippingPatch patch)
		{
			ClearMeshInstances();
			if (patch.ClippingObjects != null)
			{
				foreach (var clippingObjectId in patch.ClippingObjects)
				{
					Spatial targetActor = parent.App.FindActor(clippingObjectId) as Spatial;
					AddMeshInstance(targetActor);
				}
			}
		}

		protected IEnumerable<ShaderMaterial> ShaderMaterials()
		{
			foreach (var meshInstance in meshInstances)
			{
				if (!meshInstance.IsVisibleInTree()) continue;
				if (meshInstance.Get("material_override") is ShaderMaterial shaderMaterial)
					yield return shaderMaterial;
				else if (meshInstance.Get("mesh") is Mesh mesh)
				{
					var materialCount = mesh.GetSurfaceCount();
					for (int i = 0; i < materialCount; i++)
					{
						var material = mesh.SurfaceGetMaterial(i);
						if (material is ShaderMaterial meshMaterial)
							yield return meshMaterial;
					}
				}
				else
				{
					var materialCount = (int)meshInstance.Call("get_surface_material_count");
					for (int i = 0; i < materialCount; i++)
					{
						var material = meshInstance.Call("get_surface_material", i);
						if (material is ShaderMaterial meshInstanceMaterial)
							yield return meshInstanceMaterial;
					}
				}
			}
		}

		public override void _Ready()
		{
			parent = GetParent() as IActor;
		}

		public virtual void ClearMeshInstances()
		{
			meshInstances.Clear();
		}

		public virtual void AddMeshInstance(Spatial root)
		{
			MWGOTreeWalker.VisitTree(root, node =>
			{
				if (node is MeshInstance || node.IsClass("MeshInstance"))
				{
					meshInstances.Add((Spatial)node);
				}
			});
		}

		public virtual void RemoveMeshInstance(Spatial root)
		{
			MWGOTreeWalker.VisitTree(root, node =>
			{
				if (node is MeshInstance || node.IsClass("MeshInstance"))
				{
					meshInstances.Remove((Spatial)node);
				}
			});
		}

		public IEnumerable<Spatial> GetNodesCopy()
		{
			return new List<Spatial>(meshInstances);
		}
	}
}
