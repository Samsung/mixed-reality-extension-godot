// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Godot;

using MixedRealityExtension.Util.GodotHelper;
using MRECollisionDetectionMode = MixedRealityExtension.Core.Interfaces.CollisionDetectionMode;
using MRERigidBodyConstraints = MixedRealityExtension.Core.Interfaces.RigidBodyConstraints;

namespace MixedRealityExtension.Patching.Types
{
	[JsonObject(MemberSerialization.OptOut)]
	public class RigidBodyPatch : Patchable<RigidBodyPatch>
	{
		private MRERigidBodyConstraints? _constraintFlags;
		private MRERigidBodyConstraints[] _constraints;

		[PatchProperty]
		public Vector3Patch Velocity { get; set; }

		[PatchProperty]
		public Vector3Patch AngularVelocity { get; set; }

		[PatchProperty]
		public float? Mass { get; set; }

		[PatchProperty]
		public bool? DetectCollisions { get; set; }

		[PatchProperty]
		public MRECollisionDetectionMode? CollisionDetectionMode { get; set; }

		[PatchProperty]
		public bool? UseGravity { get; set; }

		[PatchProperty]
		public bool? IsKinematic { get; set; }

		[PatchProperty]
		public MRERigidBodyConstraints[] Constraints
		{
			get
			{
				return _constraints;
			}

			set
			{
				_constraints = value;
				_constraintFlags = MRERigidBodyConstraints.None;

				foreach (var constraint in _constraints)
				{
					_constraintFlags |= constraint;
				}
			}
		}

		[JsonIgnore]
		[PatchProperty]
		public MRERigidBodyConstraints? ConstraintFlags
		{
			get
			{
				return _constraintFlags;
			}

			set
			{
				if (value == null)
				{
					_constraintFlags = null;
					_constraints = null;
					return;
				}

				_constraintFlags = value;

				var constraints = new List<MRERigidBodyConstraints>();
				if (_constraintFlags == MRERigidBodyConstraints.None)
				{
					constraints.Add(MRERigidBodyConstraints.None);
				}
				else
				{
					foreach (var constraintFlag in (MRERigidBodyConstraints[])Enum.GetValues(typeof(MRERigidBodyConstraints)))
					{
						if ((_constraintFlags & constraintFlag) != 0)
						{
							constraints.Add(constraintFlag);
						}
					}
				}

				_constraints = constraints.ToArray();
			}
		}

		public RigidBodyPatch()
		{ }

		internal RigidBodyPatch(RigidBody rigidbody, Spatial sceneRoot)
		{
			// Do not include Position or Rotation in the patch.
			Velocity = new Vector3Patch(sceneRoot.ToLocal(rigidbody.LinearVelocity));
			AngularVelocity = new Vector3Patch(sceneRoot.ToLocal(rigidbody.AngularVelocity));
			Mass = rigidbody.Mass;
			DetectCollisions = !rigidbody.GetChild<CollisionShape>().Disabled;
			CollisionDetectionMode = rigidbody.ContinuousCd switch
			{
				true => MRECollisionDetectionMode.Continuous,
				false => MRECollisionDetectionMode.Discrete
			};
			UseGravity = !Mathf.IsZeroApprox(rigidbody.GravityScale);
			ConstraintFlags = rigidbody.GetMRERigidBodyConstraints();
		}

	}
}
