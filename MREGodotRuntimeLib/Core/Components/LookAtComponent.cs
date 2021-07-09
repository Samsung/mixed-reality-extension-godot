// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Patching.Types;
using System;
using Godot;

namespace MixedRealityExtension.Core.Components
{
	/// <summary>
	/// Unity Behaviour to face toward a given target object
	/// </summary>
	internal class LookAtComponent : ActorComponentBase
	{
		private Spatial _targetObject;
		private LookAtMode _lookAtMode;
		private bool _backward;
		private Actor parent;

		internal void ApplyPatch(LookAtPatch patch)
		{
			if (patch.ActorId.HasValue)
			{
				IActor targetActor = AttachedActor.App.FindActor(patch.ActorId.Value);
				if (targetActor != null)
				{
					_targetObject = targetActor.Node3D as Spatial;
				}
				else
				{
					_targetObject = null;
				}
			}
			if (patch.Mode.HasValue)
			{
				_lookAtMode = patch.Mode.Value;
			}
			if (patch.Backward.HasValue)
			{
				_backward = patch.Backward.Value;
			}
			parent = GetParent() as Actor;
		}

		public override void _Process(float delta)
		{
			if (_lookAtMode != LookAtMode.None && _targetObject != null)
			{
				LookAt();
			}
		}

		private void LookAt()
		{
			Vector3 pos = -_targetObject.GlobalTransform.origin;
			Vector3 delta = pos - parent.GlobalTransform.origin;

			if (parent == null)
			{
				// parent should be Actor.
				return;
			}

			if (delta == Vector3.Zero)
			{
				// In case of zero-length, don't change our rotation.
				return;
			}

			if (_backward)
			{
				pos *= -1;
			}

			parent.LookAt(pos, Vector3.Up);
			if (_lookAtMode == LookAtMode.TargetY)
			{
				parent.Rotation = new Vector3(0, parent.Rotation.y, parent.Rotation.z);
			}
		}
	}
}
