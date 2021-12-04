// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Godot;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Util.GodotHelper;
using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.Core
{
	internal class TouchablePlane : Spatial, ITouchableSurface
	{
		/// <inheritdoc />
		public float DebounceThreshold { get; set; } = 0.01f;

		/// <summary>
		/// Local space forward direction
		/// </summary>
		public Vector3 LocalForward { get; protected set; } = Vector3.Forward;

		/// <summary>
		/// Local space up direction
		/// </summary>
		public Vector3 LocalUp { get; protected set; } = Vector3.Up;

		/// <summary>
		/// Returns true if the LocalForward and LocalUp vectors are orthogonal.
		/// </summary>
		/// <remarks>
		/// LocalRight is computed using the cross product and is always orthogonal to LocalForward and LocalUp.
		/// </remarks>
		public bool AreLocalVectorsOrthogonal => LocalForward.Dot(LocalUp) == 0;

		/// <summary>
		/// Local space object center
		/// </summary>
		public Vector3 LocalCenter { get; protected set; } = Vector3.Zero;

		/// <summary>
		/// Local space and gameObject right
		/// </summary>
		public Vector3 LocalRight
		{
			get
			{
				Vector3 cross = LocalUp.Cross(LocalForward);
				if (cross == Vector3.Zero)
				{
					// vectors are collinear return default right
					return Vector3.Right;
				}
				else
				{
					return cross;
				}
			}
		}

		/// <summary>
		/// Forward direction of the actor
		/// </summary>
		public Vector3 Forward => ParentActor.ToGlobal(LocalForward);

		/// <summary>
		/// Forward direction of the TouchablePlane, the press direction needs to face the
		/// camera.
		/// </summary>
		public Vector3 LocalPressDirection => LocalForward;

		/// <summary>
		/// Bounds or size of the TouchablePlane
		/// </summary>
		public Vector2 Bounds { get; protected set; } = Vector2.One;

		private Spatial ParentActor;

		public TouchablePlane(Actor actor)
		{
			ParentActor = actor;
			actor.AddChild(this);
		}

		private void ValidateProperties()
		{
			Debug.Assert(LocalForward.Length() > 0);
			Debug.Assert(LocalUp.Length() > 0);
			string hierarchy = "";//gameObject.transform.EnumerateAncestors(true).Aggregate("", (result, next) => next.gameObject.name + "=>" + result);
			if (LocalUp.LengthSquared() == 1 && LocalForward.LengthSquared() == 1)
			{
				Debug.Assert(LocalForward.Dot(LocalUp) == 0, $"localForward and localUp not perpendicular for object {hierarchy}. Did you set Local Forward correctly?");
			}

			LocalForward = LocalForward.Normalized();
			LocalUp = LocalUp.Normalized();

			Bounds = new Vector2(Mathf.Max(Bounds.x, 0), Mathf.Max(Bounds.y, 0));
		}

		/// <summary>
		/// Set local forward direction and ensure that local up is perpendicular to the new local forward and
		/// local right direction.  The forward position should be facing the camera. The direction is indicated in scene view by a
		/// white arrow in the center of the plane.
		/// </summary>
		public void SetLocalForward(Vector3 newLocalForward)
		{
			LocalForward = newLocalForward;
			LocalUp = LocalForward.Cross(LocalRight).Normalized();
		}

		/// <summary>
		/// Set new local up direction and ensure that local forward is perpendicular to the new local up and
		/// local right direction.
		/// </summary>
		public void SetLocalUp(Vector3 newLocalUp)
		{
			LocalUp = newLocalUp;
			LocalForward = LocalRight.Cross(LocalUp).Normalized();
		}

		/// <summary>
		/// Set the position (center) of the NearInteractionTouchable plane relative to the actor.
		/// The position of the plane should be in front of the actor.
		/// </summary>
		public void SetLocalCenter(Vector3 newLocalCenter)
		{
			LocalCenter = newLocalCenter;
		}

		/// <summary>
		/// Set the size (bounds) of the TouchablePlane.
		/// </summary>
		public void SetBounds(Vector2 newBounds)
		{
			Bounds = newBounds;
		}

		/// <summary>
		/// Adjust the bounds, local center and local forward to match a given box collider.  This method
		/// also changes the size of the box collider attached to the actor.
		/// Default Behavior:  if touchableCollider is null at runtime, the object's box collider will be used
		/// to size and place the TouchablePlane in front of the actor
		/// </summary>
		public void SetTouchableCollisionShape(CollisionObject collisionObject)
		{
			CollisionShape collisionShape = collisionObject.GetChild<CollisionShape>();
			if (collisionShape != null && collisionShape.Shape is BoxShape boxShape)
			{
				Vector2 adjustedSize = new Vector2(
							Math.Abs(boxShape.Extents.Dot(LocalRight) * 2),
							Math.Abs(boxShape.Extents.Dot(LocalUp) * 2));

				SetBounds(adjustedSize);

				// Set x and y center to match the newCollider but change the position of the
				// z axis so the plane is always in front of the object
				SetLocalCenter(collisionShape.Transform.origin + LocalForward * boxShape.Extents);
			}
			else
			{
				GD.PushWarning("BoxCollider is null, cannot set bounds of TouchableComponent");
			}
		}

		/// <inheritdoc />
		public float DistanceToTouchable(Vector3 samplePoint, out Vector3 normal)
		{
			normal = Forward;

			Vector3 localPoint = ParentActor.ToLocal(samplePoint) - LocalCenter;

			// Get surface coordinates
			Vector3 planeSpacePoint = new Vector3(
				localPoint.Dot(LocalRight),
				localPoint.Dot(LocalUp),
				localPoint.Dot(LocalForward));

			// touchables currently can only be touched within the bounds of the rectangle.
			// We return infinity to ensure that any point outside the bounds does not get touched.
			if (planeSpacePoint.x < -Bounds.x / 2 ||
				planeSpacePoint.x > Bounds.x / 2 ||
				planeSpacePoint.y < -Bounds.y / 2 ||
				planeSpacePoint.y > Bounds.y / 2)
			{
				return float.PositiveInfinity;
			}

			// Scale back to 3D space
			planeSpacePoint = ParentActor.GlobalTransform.basis.Scale * planeSpacePoint;
			return planeSpacePoint.z;
		}

		internal void ApplyPatch(TouchablePatch patch)
		{
			switch (patch.Direction)
			{
				case TouchableDirection.Forward:
					SetLocalForward(-Vector3.Forward);
					break;
				case TouchableDirection.Back:
					SetLocalForward(Vector3.Forward);
					break;
				case TouchableDirection.Up:
					SetLocalForward(Vector3.Up);
					break;
				case TouchableDirection.Down:
					SetLocalForward(Vector3.Down);
					break;
				case TouchableDirection.Right:
					SetLocalForward(Vector3.Right);
					break;
				case TouchableDirection.Left:
					SetLocalForward(Vector3.Left);
					break;
			}

			var collisionObject = ParentActor.GetChild<CollisionObject>();
			if (collisionObject != null)
			{
				SetTouchableCollisionShape(collisionObject);
			}
			Bounds = Bounds.GetPatchApplied(new MWVector2(Bounds.x, Bounds.y).ApplyPatch(patch.Bounds));

			ValidateProperties();
		}
	}
}