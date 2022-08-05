// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Godot;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.GodotHelper;

namespace MixedRealityExtension.Core
{
	public abstract partial class ClippingBase : Node3D
	{
		private List<Node3D> meshInstances = new List<Node3D>();
		private IActor parent;

		public AABB Bounds => new AABB(GlobalTransform.origin - GlobalTransform.basis.Scale / 2, GlobalTransform.basis.Scale);

		internal void ApplyPatch(ClippingPatch patch)
		{
			ClearMeshInstance3Ds();
			if (patch.ClippingObjects != null)
			{
				foreach (var clippingObjectId in patch.ClippingObjects)
				{
					Node3D targetActor = parent.App.FindActor(clippingObjectId) as Node3D;
					AddMeshInstance3D(targetActor);
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

		public virtual void ClearMeshInstance3Ds()
		{
			meshInstances.Clear();
		}

		public virtual void AddMeshInstance3D(Node3D root)
		{
			MWGOTreeWalker.VisitTree(root, node =>
			{
				if (node is MeshInstance3D || node.IsClass("MeshInstance3D"))
				{
					meshInstances.Add((Node3D)node);
				}
			});
		}

		public virtual void RemoveMeshInstance3D(Node3D root)
		{
			MWGOTreeWalker.VisitTree(root, node =>
			{
				if (node is MeshInstance3D || node.IsClass("MeshInstance3D"))
				{
					meshInstances.Remove((Node3D)node);
				}
			});
		}

		public IEnumerable<Node3D> GetNodesCopy()
		{
			return new List<Node3D>(meshInstances);
		}
	}
}
