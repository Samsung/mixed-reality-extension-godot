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
		}

		void Update()
		{
/*FIXME
			if (_lookAtMode != LookAtMode.None && _targetObject != null)
			{
				var rotation = CalcRotation();
				if (rotation.HasValue)
				{
					rotation = rotation.Value;
				}
			}
*/
		}
/*FIXME
		private Quat? CalcRotation()
		{
			Vector3 pos = _targetObject.Transform.origin;
			Vector3 delta = pos - Transform.origin;

			if (delta == Vector3.Zero)
			{
				// In case of zero-length, don't change our rotation.
				return null;
			}

			if (_backward)
			{
				delta *= -1;
			}

			//Quat look = _targetObject.Transform.LookingAt(delta, Vector3.Up).origin;

			if (_lookAtMode == LookAtMode.TargetY)
			{
				look = Quat.Euler(0, look.eulerAngles.y, look.eulerAngles.z);
			}

			return look;
		}
*/
	}
}
