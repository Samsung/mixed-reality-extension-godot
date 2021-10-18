// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using Godot;

namespace Assets.Scripts.Tools
{
	public class ButtonTool : TargetTool
	{
		private PokeTool pokeTool = new PokeTool();
		protected override void UpdateTool(InputSource inputSource)
		{
			base.UpdateTool(inputSource);

			if (Target == null || !Godot.Object.IsInstanceValid(Target))
			{
				return;
			}

			pokeTool.UpdateTool(inputSource);

			if (Input.IsActionPressed("Fire1"))
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
			if (oldTarget != null && pokeTool.CurrentTouchableObjectDown == oldTarget)
			{
				pokeTool.OnTargetChanged(inputSource);
			}
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

		protected override Spatial FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			Spatial target = pokeTool.FindTarget(inputSource);
			if (target != null)
			{
				hitPoint = pokeTool.Position;
				return target;
			}
			return base.FindTarget(inputSource, out hitPoint);
		}
	}
}
