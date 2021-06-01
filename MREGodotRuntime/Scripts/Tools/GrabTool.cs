// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using System;
using Godot;

namespace Assets.Scripts.Tools
{
	public enum GrabState
	{
		Grabbed,
		Released
	}

	public class GrabStateChangedArgs
	{
		public GrabState OldGrabState { get; }
		public GrabState NewGrabState { get; }
		public InputSource InputSource { get; }

		public GrabStateChangedArgs(GrabState oldGrabState, GrabState newGrabState, InputSource inputSource)
		{
			OldGrabState = oldGrabState;
			NewGrabState = newGrabState;
			InputSource = inputSource;
		}
	}

	public class GrabTool: IDisposable
	{
		private Spatial _manipulator;
		private Node _previousParent;
		private Vector3 _manipulatorPosInToolSpace;
		private Vector3 _manipulatorupInToolSpace;
		private Vector3 _manipulatorLookAtPosInToolSpace;
		private InputSource _currentInputSource;

		public bool GrabActive => CurrentGrabbedTarget != null && Godot.Object.IsInstanceValid(CurrentGrabbedTarget);

		public Spatial CurrentGrabbedTarget { get; private set; }

		public EventHandler<GrabStateChangedArgs> GrabStateChanged { get; set; }

		public void Update(InputSource inputSource, Spatial target)
		{
			if (target == null || !Godot.Object.IsInstanceValid(target))
			{
				return;
			}

			if (Input.IsActionPressed("Fire2"))
			{
				var grabBehavior = target.GetBehavior<TargetBehavior>();
				if (grabBehavior != null)
				{
					var mwUser = grabBehavior.GetMWUnityUser(inputSource.UserNode);
					if (mwUser != null)
					{
						grabBehavior.Context.StartGrab(mwUser);
						grabBehavior.IsGrabbed = true;
					}
				}

				StartGrab(inputSource, target);
				GrabStateChanged?.Invoke(this, new GrabStateChangedArgs(GrabState.Released, GrabState.Grabbed, inputSource));
			}
			else if (Input.IsActionJustReleased("Fire2"))
			{
				var grabBehavior = target.GetBehavior<TargetBehavior>();
				if (grabBehavior != null)
				{
					var mwUser = grabBehavior.GetMWUnityUser(inputSource.UserNode);
					if (mwUser != null)
					{
						grabBehavior.Context.EndGrab(mwUser);
						grabBehavior.IsGrabbed = false;
					}
				}

				EndGrab();
				GrabStateChanged?.Invoke(this, new GrabStateChangedArgs(GrabState.Grabbed, GrabState.Released, inputSource));
			}

			if (GrabActive)
			{
				UpdatePosition();
				UpdateRotation();
			}
		}

		private void StartGrab(InputSource inputSource, Spatial target)
		{
			if (GrabActive ||target == null || !Godot.Object.IsInstanceValid(target))
			{
				return;
			}

			CurrentGrabbedTarget = target;
			_currentInputSource = inputSource;

			var targetTransform = CurrentGrabbedTarget.GlobalTransform;

			_manipulator = new Spatial() { Name = "manipulator" };
			inputSource.GetTree().Root.AddChild(_manipulator);
			_manipulator.GlobalTransform = targetTransform;

			_previousParent = CurrentGrabbedTarget.GetParent();
			_previousParent.RemoveChild(CurrentGrabbedTarget);
			_manipulator.AddChild(CurrentGrabbedTarget);
			CurrentGrabbedTarget.GlobalTransform = targetTransform;

			_manipulatorPosInToolSpace = _currentInputSource.ToLocal(_manipulator.GlobalTransform.origin);
			_manipulatorupInToolSpace = _currentInputSource.ToLocal(_manipulator.GlobalTransform.basis.y);
			_manipulatorLookAtPosInToolSpace = _currentInputSource.ToLocal(_manipulator.GlobalTransform.origin - _manipulator.GlobalTransform.basis.z);
		}

		private void EndGrab()
		{
			if (!GrabActive)
			{
				return;
			}
			var currentTargetGolbalTransform = CurrentGrabbedTarget.GlobalTransform;
			_manipulator.RemoveChild(CurrentGrabbedTarget);
			_previousParent.AddChild(CurrentGrabbedTarget);
			CurrentGrabbedTarget.GlobalTransform = currentTargetGolbalTransform;
			CurrentGrabbedTarget = null;

			_manipulator.QueueFree();
			_manipulator = null;
		}

		private void UpdatePosition()
		{
			Vector3 targetPosition = _currentInputSource.ToGlobal(_manipulatorPosInToolSpace);
			_manipulator.GlobalTransform = new Transform(_manipulator.GlobalTransform.basis, targetPosition);
		}

		private void UpdateRotation()
		{
			Vector3 targetLookAtPos = _currentInputSource.ToGlobal(_manipulatorLookAtPosInToolSpace);
			Vector3 targetUp = _currentInputSource.ToGlobal(_manipulatorupInToolSpace);
			/*FIXME
			_manipulator.transform.rotation = Quaternion.LookRotation(targetLookAtPos - _manipulator.transform.position, targetUp);
			*/
		}

		public void Dispose()
		{
			if (_manipulator != null)
			{
				_manipulator.QueueFree();
			}
		}
	}
}
