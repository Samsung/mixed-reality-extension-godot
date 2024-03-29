// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.GodotHelper;
using System;
using System.Collections.Generic;
using Godot;

using GodotRigidBody = Godot.RigidBody3D;
using MRECollisionDetectionMode = MixedRealityExtension.Core.Interfaces.CollisionDetectionMode;
using MRERigidBodyConstraints = MixedRealityExtension.Core.Interfaces.RigidBodyConstraints;

namespace MixedRealityExtension.Core
{
	internal class RigidBody : IRigidBody3D
	{
		private readonly Node3D _sceneRoot;
		private readonly GodotRigidBody _rigidbody;

		private Queue<Action<GodotRigidBody>> _updateActions = new Queue<Action<GodotRigidBody>>();

		/// <inheritdoc />
		public MWVector3 Velocity { get; set; } = new MWVector3();

		/// <inheritdoc />
		public MWVector3 AngularVelocity { get; set; } = new MWVector3();

		/// <inheritdoc />
		public float Mass { get; set; }

		/// <inheritdoc />
		public bool DetectCollisions { get; set; }

		/// <inheritdoc />
		public MRECollisionDetectionMode CollisionDetectionMode { get; set; }

		/// <inheritdoc />
		public bool UseGravity { get; set; }

		/// <inheritdoc />
		public bool IsKinematic { get; set; }

		/// <inheritdoc />
		public MRERigidBodyConstraints ConstraintFlags { get; set; }

		internal RigidBody(GodotRigidBody rigidbody, Node3D sceneRoot)
		{
			_sceneRoot = sceneRoot;
			_rigidbody = rigidbody;
			// Read initial values
			Update(rigidbody);
		}
/*FIXME
		/// <inheritdoc />
		public void RigidBody3DMovePosition(MWVector3 position)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.MovePosition(_sceneRoot.ToGlobal(position.ToVector3()));
				});
		}

		/// <inheritdoc />
		public void RigidBody3DMoveRotation(MWQuaternion rotation)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.MoveRotation(_sceneRoot.rotation * rotation.ToQuaternion());
				});
		}
*/
		/// <inheritdoc />
		public void RigidBody3DAddForce(MWVector3 force)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.ApplyCentralForce(_sceneRoot.ToGlobal(force.ToVector3()));
				});
		}

		/// <inheritdoc />
		public void RigidBody3DAddForceAtPosition(MWVector3 force, MWVector3 position)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.ApplyImpulse(_sceneRoot.ToGlobal(position.ToVector3()), _sceneRoot.ToGlobal(force.ToVector3()));
				});
		}

		/// <inheritdoc />
		public void RigidBody3DAddTorque(MWVector3 torque)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.ApplyTorque(_sceneRoot.ToGlobal(torque.ToVector3()));
				});
		}

		/// <inheritdoc />
		public void RigidBody3DAddRelativeTorque(MWVector3 relativeTorque)
		{
			_updateActions.Enqueue(
				(rigidBody) =>
				{
					rigidBody.ApplyTorque(_sceneRoot.ToGlobal(relativeTorque.ToVector3()));
				});
		}

		internal void Update()
		{
			if (_rigidbody == null)
			{
				return;
			}

			try
			{
				while (_updateActions.Count > 0)
				{
					_updateActions.Dequeue()(_rigidbody);
				}
			}
			catch (Exception e)
			{
				MREAPI.Logger.LogError($"Failed to perform async update of rigid body.  Exception: {e.Message}\nStack Trace: {e.StackTrace}");
			}
		}

		internal void Update(GodotRigidBody rigidbody)
		{
			Velocity.FromGodotVector3(_sceneRoot.ToLocal(rigidbody.LinearVelocity));
			AngularVelocity.FromGodotVector3(_sceneRoot.ToLocal(rigidbody.AngularVelocity));

			// No need to read Position or Rotation. They're write-only from the patch to the component.
			Mass = rigidbody.Mass;
			DetectCollisions = !rigidbody.GetChild<CollisionShape3D>()?.Disabled ?? false;
			CollisionDetectionMode = rigidbody.ContinuousCd switch
			{
				true => MRECollisionDetectionMode.Continuous,
				false => MRECollisionDetectionMode.Discrete
			};
			UseGravity = !Mathf.IsZeroApprox(rigidbody.GravityScale);
			ConstraintFlags = rigidbody.GetMRERigidBodyConstraints();
		}

		internal void ApplyPatch(RigidBodyPatch patch, bool patchVelocities)
		{
			// Apply any changes made to the state of the mixed reality extension runtime version of the rigid body.

			if (patchVelocities)
			{
				if (patch.Velocity != null && patch.Velocity.IsPatched())
				{
					_rigidbody.LinearVelocity = _rigidbody.LinearVelocity.GetPatchApplied(_sceneRoot.ToGlobal(Velocity.ApplyPatch(patch.Velocity).ToVector3()));
				}
				if (patch.AngularVelocity != null && patch.AngularVelocity.IsPatched())
				{
					var angularVelocity =_sceneRoot.ToGlobal(AngularVelocity.ApplyPatch(patch.AngularVelocity).ToVector3());
					angularVelocity.x = Mathf.DegToRad(angularVelocity.x);
					angularVelocity.y = Mathf.DegToRad(angularVelocity.y);
					angularVelocity.z = Mathf.DegToRad(angularVelocity.z);
					_rigidbody.AngularVelocity = _rigidbody.AngularVelocity.GetPatchApplied(angularVelocity);
				}
			}

			if (patch.Mass.HasValue)
			{
				_rigidbody.Mass = _rigidbody.Mass.GetPatchApplied(Mass.ApplyPatch(patch.Mass));
			}
			if (patch.DetectCollisions.HasValue)
			{
				var collisionShape = _rigidbody.GetChild<CollisionShape3D>();
				collisionShape.Disabled = collisionShape.Disabled.GetPatchApplied(!DetectCollisions.ApplyPatch(patch.DetectCollisions));
			}
			if (patch.CollisionDetectionMode.HasValue)
			{
				switch (patch.CollisionDetectionMode)
				{
					case MRECollisionDetectionMode.Continuous:
					case MRECollisionDetectionMode.ContinuousDynamic:
						_rigidbody.ContinuousCd = true;
						break;
					case MRECollisionDetectionMode.Discrete:
						_rigidbody.ContinuousCd = false;
						break;
				}
			}
			if (patch.UseGravity.HasValue)
			{
				_rigidbody.GravityScale = (bool)patch.UseGravity ? 1 : 0;
			}
			if (patch.IsKinematic.HasValue)
			{
				IsKinematic = patch.IsKinematic.Value;
			}
			if (patch.ConstraintFlags.HasValue)
			{
				if ((bool)patch.ConstraintFlags?.HasFlag(MRERigidBodyConstraints.FreezePositionX)) _rigidbody.AxisLockLinearX = true;
				if ((bool)patch.ConstraintFlags?.HasFlag(MRERigidBodyConstraints.FreezePositionY)) _rigidbody.AxisLockLinearY = true;
				if ((bool)patch.ConstraintFlags?.HasFlag(MRERigidBodyConstraints.FreezePositionZ)) _rigidbody.AxisLockLinearZ = true;
				if ((bool)patch.ConstraintFlags?.HasFlag(MRERigidBodyConstraints.FreezeRotationX)) _rigidbody.AxisLockAngularX = true;
				if ((bool)patch.ConstraintFlags?.HasFlag(MRERigidBodyConstraints.FreezeRotationY)) _rigidbody.AxisLockAngularY = true;
				if ((bool)patch.ConstraintFlags?.HasFlag(MRERigidBodyConstraints.FreezeRotationZ)) _rigidbody.AxisLockAngularZ = true;
			}
		}

		internal void UpdateTransform(RigidBodyTransformUpdate update)
		{
			Vector3 origin = _rigidbody.GlobalTransform.origin;
			Basis basis = _rigidbody.GlobalTransform.basis;

			if (update.Position != null)
			{
				origin = update.Position.Value;
			}
			if (update.Rotation != null)
			{
				basis = new Basis(update.Rotation.Value);
			}
			_rigidbody.GlobalTransform = new Transform3D(basis, origin);
		}

		internal void SynchronizeEngine(RigidBodyTransformUpdate update)
		{
			_updateActions.Enqueue((rigidBody) => UpdateTransform(update));
		}

		internal void SynchronizeEngine(RigidBodyPatch patch, bool patchVelocities)
		{
			_updateActions.Enqueue((rigidbody) => ApplyPatch(patch, patchVelocities));
		}

		internal struct RigidBodyTransformUpdate
		{
			internal Vector3? Position { get; set; }

			internal Quaternion? Rotation { get; set; }
		}
	}
}
