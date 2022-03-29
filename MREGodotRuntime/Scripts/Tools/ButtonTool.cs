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

			if (!Godot.Object.IsInstanceValid(Target) || IsNearObject || !inputSource.PinchChaged)
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
						startOffset = inputSource.GlobalTransform.origin - CurrentTargetPoint;
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
			Spatial oldTarget,
			Vector3 oldTargetPosition,
			Spatial newTarget,
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

			if (oldTarget != null && Godot.Object.IsInstanceValid(oldTarget))
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

			if (newTarget != null && Godot.Object.IsInstanceValid(newTarget))
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

		protected override Spatial FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			if (pressed)
			{
				hitPoint = inputSource.GlobalTransform.origin - startOffset;
				return Target;
			}
			return base.FindTarget(inputSource, out hitPoint);
		}
	}
}
