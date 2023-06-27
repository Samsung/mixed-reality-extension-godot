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
		private List<GeometryInstance3D> meshInstances = new List<GeometryInstance3D>();
		private IActor parent;

		public Aabb Bounds => new Aabb(GlobalTransform.Origin - GlobalTransform.Basis.Scale / 2, GlobalTransform.Basis.Scale);

		internal void ApplyPatch(ClippingPatch patch)
		{
			ClearMeshInstances();
			if (patch.ClippingObjects != null)
			{
				foreach (var clippingObjectId in patch.ClippingObjects)
				{
					Node3D targetActor = parent.App.FindActor(clippingObjectId) as Node3D;
					AddMeshInstance(targetActor);
				}
			}
		}

		protected IEnumerable<Rid> ShaderMaterialRIDs()
		{
			foreach (var meshInstance in meshInstances)
			{
				if (!meshInstance.IsVisibleInTree()) continue;

				var surfaceCount = RenderingServer.MeshGetSurfaceCount(meshInstance.GetBase());
				for (int i = 0; i < surfaceCount; i++)
				{
					yield return RenderingServer.MeshSurfaceGetMaterial(meshInstance.GetBase(), i);
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

		public virtual void AddMeshInstance(Node3D root)
		{
			MWGOTreeWalker.VisitTree(root, node =>
			{
				if (node is GeometryInstance3D n)
				{
					meshInstances.Add(n);
				}
			});
		}

		public virtual void RemoveMeshInstance(Node3D root)
		{
			MWGOTreeWalker.VisitTree(root, node =>
			{
				if (node is GeometryInstance3D n)
				{
					meshInstances.Remove(n);
				}
			});
		}

		public IEnumerable<Node3D> GetNodesCopy()
		{
			return new List<Node3D>(meshInstances);
		}
	}
}
