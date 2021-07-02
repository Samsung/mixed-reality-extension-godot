// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Util.GodotHelper;
using System;
using Godot;

using GodotCollisionShape = Godot.CollisionShape;

namespace MixedRealityExtension.Core
{
	/// <summary>
	/// Abstract class that represents the collider geometry.
	/// </summary>
	public abstract class ColliderGeometry
	{
		/// <summary>
		/// The shape of the collider. <see cref="ColliderType"/>
		/// </summary>
		public abstract ColliderType Shape { get; }

		internal abstract void Patch(MixedRealityExtensionApp app, GodotCollisionShape collider);
	}

	/// <summary>
	/// Class that represents the sphere geometry for a sphere collider.
	/// </summary>
	public class SphereColliderGeometry : ColliderGeometry
	{
		/// <inheritdoc />
		public override ColliderType Shape => ColliderType.Sphere;

		/// <summary>
		/// The center of the sphere collider geometry.
		/// </summary>
		public MWVector3 Center { get; set; }

		/// <summary>
		/// The radius of the sphere collider geometry.
		/// </summary>
		public float? Radius { get; set; }

		internal override void Patch(MixedRealityExtensionApp app, GodotCollisionShape collider)
		{
			if (collider.Shape is SphereShape sphereCollider)
			{
				if (Center != null)
				{
					Vector3 newCenter;
					newCenter.x = Center.X;
					newCenter.y = Center.Y;
					newCenter.z = Center.Z;
					collider.Transform = new Transform(Basis.Identity, newCenter);
				}

				if (Radius != null)
				{
					sphereCollider.Radius = Radius.Value;
				}
			}
		}
	}

	/// <summary>
	/// Class that represents the box geometry of a box collider.
	/// </summary>
	public class BoxColliderGeometry : ColliderGeometry
	{
		/// <inheritdoc />
		public override ColliderType Shape => ColliderType.Box;

		/// <summary>
		/// The size of the box collider geometry.
		/// </summary>
		public MWVector3 Size { get; set; }

		/// <summary>
		/// The center of the box collider geometry.
		/// </summary>
		public MWVector3 Center { get; set; }

		internal override void Patch(MixedRealityExtensionApp app, GodotCollisionShape collider)
		{
			if (collider.Shape is BoxShape boxShape)
			{
				if (Center != null)
				{
					Vector3 newCenter;
					newCenter.x = Center.X;
					newCenter.y = Center.Y;
					newCenter.z = Center.Z;
					collider.Transform = new Transform(Basis.Identity, newCenter);
				}

				if (Size != null)
				{
					Vector3 newSize;
					newSize.x = Size.X;
					newSize.y = Size.Y;
					newSize.z = Size.Z;
					boxShape.Extents = newSize;
				}
			}
		}
	}

	/// <summary>
	/// Class that represents the mesh geometry of a mesh collider.
	/// </summary>
	public class MeshColliderGeometry : ColliderGeometry
	{
		/// <inheritdoc />
		public override ColliderType Shape => ColliderType.Mesh;

		/// <summary>
		/// The asset ID of the collider's mesh
		/// </summary>
		public Guid MeshId { get; set; }

		internal override void Patch(MixedRealityExtensionApp app, GodotCollisionShape collider)
		{
			if (collider.Shape is ConcavePolygonShape concavePolygonShape)
			{
				Patch(app, concavePolygonShape);
			}
		}

		private void Patch(MixedRealityExtensionApp app, ConcavePolygonShape concavePolygonShape)
		{
			var tempId = MeshId;
			app.AssetManager.OnSet(MeshId, asset =>
			{
				if (MeshId != tempId) return;
				concavePolygonShape.Data = (Vector3[])(asset.Asset as Mesh).SurfaceGetArrays(0)[0];
			});
		}
	}

	/// <summary>
	/// Class that describes a cylinder-shaped collision volume
	/// </summary>
	public class CylinderColliderGeometry : ColliderGeometry
	{
		/// <inheritdoc />
		public override ColliderType Shape => ColliderType.Cylinder;

		/// <summary>
		/// The centerpoint of the collider in local space
		/// </summary>
		public MWVector3 Center { get; set; }

		/// <summary>
		/// The rotation of the collider in local space
		/// </summary>
		public Quat Rotation { get; set; }

		/// <summary>
		/// The bounding box of the cylinder.
		/// </summary>
		public MWVector3 Dimensions { get; set; }

		internal override void Patch(MixedRealityExtensionApp app, GodotCollisionShape collider)
		{
			if (collider.Shape is CylinderShape cylinderShape)
			{
				Vector3 newCenter = Vector3.Zero;
				Basis basis = Basis.Identity;
				if (Center != null)
				{
					newCenter.x = Center.X;
					newCenter.y = Center.Y;
					newCenter.z = Center.Z;
				}

				if (Dimensions != null)
				{
					float radius;
					float height;
					if (Mathf.IsEqualApprox(Dimensions.X, Dimensions.Y))
					{
						height = Dimensions.Z;
						radius = Dimensions.X / 2;
					}
					else if (Mathf.IsEqualApprox(Dimensions.X, Dimensions.Z))
					{
						height = Dimensions.Y;
						radius = Dimensions.X / 2;
					}
					else
					{
						height = Dimensions.X;
						radius = Dimensions.Y / 2;
					}
					cylinderShape.Radius = radius;
					cylinderShape.Height = height;

					if (Dimensions.X == height)
					{
						basis = basis.Rotated(Vector3.Forward, Mathf.Pi / 2);
					}
					else if (Dimensions.Z == height)
					{
						basis = basis.Rotated(Vector3.Right, Mathf.Pi / 2);
					}
				}
				collider.Transform = new Transform(basis, newCenter);
			}
		}
	}

	/// <summary>
	/// Class that describes a capsule-shaped collision volume
	/// </summary>
	public class CapsuleColliderGeometry : ColliderGeometry
	{
		/// <inheritdoc />
		public override ColliderType Shape => ColliderType.Capsule;

		/// <summary>
		/// The centerpoint of the collider in local space
		/// </summary>
		public MWVector3 Center { get; set; }

		/// <summary>
		/// The dimensions of the collider, with the largest component of the vector being the
		/// primary axis and height of the capsule, and the second largest the radius.
		/// </summary>
		public MWVector3 Size { get; set; }

		/// <summary>
		/// The primary axis of the capsule (x = 0, y = 1, z = 2)
		/// </summary>
		public int? Direction
		{
			get => Size?.LargestComponentIndex();

		}

		/// <summary>
		/// The height of the capsule along its primary axis, including end caps
		/// </summary>
		public float? Height
		{
			get => Size?.LargestComponentValue() - 2 * Radius;
		}

		/// <summary>
		/// The radius of the capsule
		/// </summary>
		public float? Radius
		{
			get => Size != null ? Size.SmallestComponentValue() / 2 : (float?) null;
		}

		internal override void Patch(MixedRealityExtensionApp app, GodotCollisionShape collider)
		{
			if (collider.Shape is CapsuleShape capsuleShape)
			{
				Vector3 newCenter = Vector3.Zero;
				Basis basis = Basis.Identity;
				if (Center != null)
				{
					newCenter.x = Center.X;
					newCenter.y = Center.Y;
					newCenter.z = Center.Z;
				}

				if (Size != null)
				{
					capsuleShape.Radius = Radius.Value;
					capsuleShape.Height = Height.Value;

					// default capsule Shape is Z-aligned; rotate if necessary
					if (Size.LargestComponentIndex() == 0)
					{
						basis = basis.Rotated(Vector3.Up, Mathf.Pi / 2);
					}
					else if (Size.LargestComponentIndex() == 1)
					{
						basis = basis.Rotated(Vector3.Right, Mathf.Pi / 2);
					}
				}
				collider.Transform = new Transform(basis, newCenter);
			}
		}
	}

	/// <summary>
	/// Class that represents geometry automatically generated alongside a mesh.
	/// </summary>
	public class AutoColliderGeometry : ColliderGeometry
	{
		/// <inheritdoc />
		public override ColliderType Shape => ColliderType.Auto;

		internal override void Patch(MixedRealityExtensionApp app, GodotCollisionShape collider)
		{
			// We do not accept patching for auto colliders from the app.
		}
	}
}
