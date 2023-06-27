// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Godot;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Util.GodotHelper;
using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.Core
{
	internal partial class TouchablePlane : Node3D, ITouchableSurface
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

		/// <inheritdoc/>
		public CollisionShape3D TouchableBoxShape
		{
			get => touchableBoxShape;
			set {
				if (touchableBoxShape == value)
					return;

				if (value != null && value.Shape is BoxShape3D boxShape)
				{
					touchableBoxShape = value;
					// Set x and y center to match the newCollider but change the position of the
					// z axis so the plane is always in front of the object
					SetLocalCenter(touchableBoxShape.Transform.Origin + LocalForward * boxShape.Size / 2);
				}
				else
				{
					GD.PushWarning("TouchableBoxShape is not BoxShape3D, cannot set TouchableBoxShape.");
				}
			}
		}

		private CollisionShape3D touchableBoxShape;
		private Node3D ParentActor;

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

			// Scale back to 3D space
			planeSpacePoint = ParentActor.GlobalTransform.Basis.Scale * planeSpacePoint;
			return planeSpacePoint.Z;
		}

		internal void ApplyPatch(TouchablePatch patch)
		{
			switch (patch.Direction)
			{
				case TouchableDirection.Forward:
					SetLocalForward(Vector3.Forward);
					break;
				case TouchableDirection.Back:
					SetLocalForward(-Vector3.Forward);
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

			var collisionObject = ParentActor.GetChild<CollisionObject3D>();
			if (collisionObject != null)
			{
				TouchableBoxShape = collisionObject.GetChild<CollisionShape3D>();
			}

			ValidateProperties();
		}
	}
}