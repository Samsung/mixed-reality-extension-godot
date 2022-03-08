// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using Godot;
using Microsoft.MixedReality.Toolkit.Input;

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
						startOffset = inputSource.Hand.GlobalTransform.origin - CurrentTargetPoint;
						buttonBehavior.Context.StartButton(mwUser, CurrentTargetPoint);
						inputSource.Cursor.Color = new Color(1, 0, 0);
					}
				}
				pressed = true;

				Target.HandleEvent<IMixedRealityPointerHandler>(nameof(IMixedRealityPointerHandler.OnPointerDown),
															new MixedRealityPointerEventData(this, CurrentTargetPoint));
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
					}
				}
				pressed = false;

				Target.HandleEvent<IMixedRealityPointerHandler>(nameof(IMixedRealityPointerHandler.OnPointerUp),
															new MixedRealityPointerEventData(this, CurrentTargetPoint));
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

		protected override void OnTargetPointUpdated(Vector3 point)
		{
			if (pressed)
			{
				Target.HandleEvent<IMixedRealityPointerHandler>(nameof(IMixedRealityPointerHandler.OnPointerDragged),
														new MixedRealityPointerEventData(this, CurrentTargetPoint));
			}
		}

		protected override Spatial FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			if (pressed)
			{
				hitPoint = inputSource.Hand.GlobalTransform.origin - startOffset;
				return Target;
			}
			return base.FindTarget(inputSource, out hitPoint);
		}
	}
}
