// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using Godot;
using Microsoft.MixedReality.Toolkit.Input;
using MixedRealityExtension.Util.GodotHelper;

namespace Assets.Scripts.Tools
{
	public class ButtonTool : TargetTool
	{
		private bool pressed;
		protected override void UpdateTool(InputSource inputSource)
		{
			base.UpdateTool(inputSource);

			if (Target == null || !Godot.Object.IsInstanceValid(Target))
			{
				return;
			}

			if (Input.IsActionJustPressed("Fire1"))
			{
				var buttonBehavior = Target.GetBehavior<ButtonBehavior>();
				if (buttonBehavior != null)
				{
					var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserNode);
					if (mwUser != null)
					{
						buttonBehavior.Context.StartButton(mwUser, CurrentTargetPoint);
						((SpatialMaterial)inputSource.CollisionPoint.MaterialOverride).AlbedoColor = new Color(1, 0, 0);
					}
				}
				pressed = true;
				var handler = IMixedRealityEventHandler.FindEventHandler<IMixedRealityPointerHandler>(Target);
				handler?.OnPointerDown(new MixedRealityPointerEventData(this, CurrentTargetPoint));
			}
			else if (Input.IsActionJustReleased("Fire1"))
			{
				var buttonBehavior = Target.GetBehavior<ButtonBehavior>();
				if (buttonBehavior != null)
				{
					var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserNode);
					if (mwUser != null)
					{
						buttonBehavior.Context.EndButton(mwUser, CurrentTargetPoint);
						buttonBehavior.Context.Click(mwUser, CurrentTargetPoint);
					}
				}
				pressed = false;
				var handler = IMixedRealityEventHandler.FindEventHandler<IMixedRealityPointerHandler>(Target);
				handler?.OnPointerUp(new MixedRealityPointerEventData(this, CurrentTargetPoint));
			}
			else
			{
				SpatialMaterial material = (SpatialMaterial)inputSource.CollisionPoint.MaterialOverride;
				material.AlbedoColor = new Color(1, 1, 1);
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
				var handler = IMixedRealityEventHandler.FindEventHandler<IMixedRealityPointerHandler>(Target);
				handler?.OnPointerDragged(new MixedRealityPointerEventData(this, CurrentTargetPoint));
			}
		}
	}
}
