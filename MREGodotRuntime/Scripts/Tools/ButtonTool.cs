// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using Godot;
using MixedRealityExtension.Core.Interfaces;

namespace Assets.Scripts.Tools
{
	public class ButtonTool : TargetTool
	{
		private bool pressed;
		private Vector3 startOffset;

		protected override void UpdateTool(InputSource inputSource)
		{
			base.UpdateTool(inputSource);

			if (!Godot.GodotObject.IsInstanceValid(Target) || IsNearObject || !inputSource.PinchChaged)
			{
				return;
			}

			if (inputSource.IsPinching)
			{
				var buttonBehavior = Target.GetBehavior<ButtonBehavior>();
				if (buttonBehavior != null)
				{
					var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserNode);
					if (mwUser != null)
					{
						startOffset = inputSource.GlobalTransform.Origin - CurrentTargetPoint;
						buttonBehavior.Context.StartButton(mwUser, CurrentTargetPoint);
						inputSource.Cursor.Color = new Color(1, 0, 0);

						if (Target.HasUserSignal("pointer_down"))
							Target.EmitSignal("pointer_down", inputSource, inputSource.UserNode, CurrentTargetPoint);
					}
				}
				pressed = true;
			}
			else
			{
				var buttonBehavior = Target.GetBehavior<ButtonBehavior>();
				if (buttonBehavior != null)
				{
					var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserNode);
					if (mwUser != null)
					{
						buttonBehavior.Context.EndButton(mwUser, CurrentTargetPoint);
						buttonBehavior.Context.Click(mwUser, CurrentTargetPoint);
						inputSource.Cursor.Color = new Color(1, 1, 1);

						if (Target.HasUserSignal("pointer_up"))
							Target.EmitSignal("pointer_up", inputSource, inputSource.UserNode, CurrentTargetPoint);
					}
				}
				pressed = false;
			}
		}

		protected override void OnTargetChanged(
			Node3D oldTarget,
			Vector3 oldTargetPosition,
			Node3D newTarget,
			Vector3 newTargetPosition,
			TargetBehavior newBehavior,
			InputSource inputSource)
		{
			base.OnTargetChanged(
				oldTarget,
				oldTargetPosition,
				newTarget,
				newTargetPosition,
				newBehavior,
				inputSource);

			if (oldTarget != null && Godot.GodotObject.IsInstanceValid(oldTarget))
			{
				var oldBehavior = oldTarget.GetBehavior<ButtonBehavior>();
				if (oldBehavior != null)
				{
					var mwUser = oldBehavior.GetMWUnityUser(inputSource.UserNode);
					if (mwUser != null)
					{
						oldBehavior.Context.EndHover(mwUser, oldTargetPosition);
					}
				}
			}

			if (newTarget != null && Godot.GodotObject.IsInstanceValid(newTarget))
			{
				var newButtonBehavior = newBehavior as ButtonBehavior;
				if (newButtonBehavior != null)
				{
					var mwUser = newButtonBehavior.GetMWUnityUser(inputSource.UserNode);
					if (mwUser != null)
					{
						newButtonBehavior.Context.StartHover(mwUser, newTargetPosition);
					}
				}
			}
		}

		protected override void OnTargetPointUpdated(InputSource inputSource, Vector3 point)
		{
			if (pressed)
			{
				var buttonBehavior = Target.GetBehavior<ButtonBehavior>();
				if (buttonBehavior != null)
				{
					var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserNode);
					if (mwUser != null)
					{
						if (Target.HasUserSignal("pointer_dragged"))
							Target.EmitSignal("pointer_dragged", inputSource, inputSource.UserNode, CurrentTargetPoint);
					}
				}
			}
		}

		protected override Node3D FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			if (pressed)
			{
				hitPoint = inputSource.GlobalTransform.Origin - startOffset;
				return Target;
			}
			return base.FindTarget(inputSource, out hitPoint);
		}
	}
}
