// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using MixedRealityExtension.Util.GodotHelper;
using MixedRealityExtension.Core.Interfaces;

using Godot;
using Godot.Collections;

namespace Assets.Scripts.Tools
{
	public class TargetTool : Tool
	{
		private PokeTool pokeTool = new PokeTool();
		private GrabTool grabTool = new GrabTool();
		private TargetBehavior _currentTargetBehavior;

		public bool IsNearObject;

		public Spatial Target { get; private set; }

		public bool TargetGrabbed => grabTool.GrabActive;

		protected Vector3 CurrentTargetPoint { get; private set; }

		public TargetTool()
		{
			grabTool.GrabStateChanged += OnGrabStateChanged;
		}

		public override void CleanUp()
		{
			grabTool.GrabStateChanged -= OnGrabStateChanged;
		}

		public override void OnToolHeld(InputSource inputSource)
		{
			base.OnToolHeld(inputSource);
			pokeTool.OnToolHeld(inputSource);
			grabTool.OnToolHeld(inputSource);

			Vector3? hitPoint;
			var newTarget = FindTarget(inputSource, out hitPoint);
			if (newTarget == null || !Godot.Object.IsInstanceValid(newTarget))
			{
				return;
			}

			var newBehavior = newTarget.GetBehavior<TargetBehavior>();

			OnTargetChanged(
				Target,
				CurrentTargetPoint,
				newTarget,
				hitPoint.Value,
				newBehavior,
				inputSource);
		}

		public override void OnToolDropped(InputSource inputSource)
		{
			base.OnToolDropped(inputSource);
			pokeTool.OnToolDropped(inputSource);

			OnTargetChanged(
				Target,
				CurrentTargetPoint,
				null,
				Vector3.Zero,
				null,
				inputSource);
		}

		protected override void UpdateTool(InputSource inputSource)
		{
			if (_currentTargetBehavior?.Grabbable ?? false)
			{
				grabTool.Update(inputSource);
				if (grabTool.GrabActive)
				{
					// If a grab is active, nothing should change about the current target.
					return;
				}
			}

			pokeTool.Update(inputSource);
			Vector3? hitPoint;

			Position = inputSource.GlobalTransform.origin;
			var newTarget = FindTarget(inputSource, out hitPoint);
			if ((Target == null || !Godot.Object.IsInstanceValid(Target)) && (newTarget == null || !Godot.Object.IsInstanceValid(newTarget)))
			{
				return;
			}

			if (Target == newTarget)
			{
				var mwUser = _currentTargetBehavior.GetMWUnityUser(inputSource.UserNode);
				if (mwUser == null)
				{
					return;
				}

				CurrentTargetPoint = hitPoint.Value;
				_currentTargetBehavior.Context.UpdateTargetPoint(mwUser, CurrentTargetPoint);
				OnTargetPointUpdated(inputSource, CurrentTargetPoint);
				return;
			}

			TargetBehavior newBehavior = null;
			if (newTarget != null && Godot.Object.IsInstanceValid(newTarget))
			{
				newBehavior = newTarget.GetBehavior<TargetBehavior>();

				// FIXME: This is workaround. Sometimes newBehavior is null even if new Target is an Actor!
				if (newBehavior == null /*&& newTarget is MixedRealityExtension.Core.Actor*/)
				{
					return;
				}

				if (newBehavior.GetDesiredToolType() != inputSource.CurrentTool.GetType())
				{
					inputSource.HoldTool(newBehavior.GetDesiredToolType());
				}
				else
				{
					OnTargetChanged(
						Target,
						CurrentTargetPoint,
						newTarget,
						hitPoint.Value,
						newBehavior,
						inputSource);
				}
			}
			else
			{
				OnTargetChanged(
					Target,
					CurrentTargetPoint,
					null,
					Vector3.Zero,
					null,
					inputSource);

				inputSource.DropTool();
			}
		}

		protected virtual void OnTargetChanged(
			Spatial oldTarget,
			Vector3 oldTargetPoint,
			Spatial newTarget,
			Vector3 newTargetPoint,
			TargetBehavior newBehavior,
			InputSource inputSource)
		{
			if (Godot.Object.IsInstanceValid(oldTarget) && pokeTool.CurrentTouchableObjectDown == oldTarget)
			{
				pokeTool.OnTargetChanged(inputSource);
			}

			if (oldTarget != null && Godot.Object.IsInstanceValid(oldTarget) && !IsNearObject)
			{
				_currentTargetBehavior.Context.EndTargeting(_currentTargetBehavior.GetMWUnityUser(inputSource.UserNode), oldTargetPoint);

				if (oldTarget.HasUserSignal("focus_exit"))
					oldTarget.EmitSignal("focus_exit", inputSource, inputSource.UserNode, oldTarget, newTarget);
			}

			if (newTarget != null && Godot.Object.IsInstanceValid(newTarget) && !IsNearObject)
			{
				newBehavior.Context.StartTargeting(newBehavior.GetMWUnityUser(inputSource.UserNode), newTargetPoint);

				if (newTarget.HasUserSignal("focus_enter"))
					newTarget.EmitSignal("focus_enter", inputSource, inputSource.UserNode, oldTarget, newTarget);
			}

			CurrentTargetPoint = newTargetPoint;
			Target = newTarget;
			_currentTargetBehavior = newBehavior;
		}

		protected virtual void OnTargetPointUpdated(InputSource inputSource, Vector3 point)
		{

		}

		protected virtual void OnGrabStateChanged(GrabState oldGrabState, GrabState newGrabState, InputSource inputSource)
		{

		}

		protected BehaviorT GetCurrentTargetBehavior<BehaviorT>() where BehaviorT : TargetBehavior
		{
			return _currentTargetBehavior as BehaviorT;
		}

		private void OnGrabStateChanged(object sender, GrabStateChangedArgs args)
		{
			OnGrabStateChanged(args.OldGrabState, args.NewGrabState, args.InputSource);
		}

		protected virtual Spatial FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			hitPoint = null;
			Spatial nearTarget = grabTool.FindTarget(inputSource, out hitPoint);
			if (!inputSource.IsPinching && nearTarget == null)
				nearTarget = pokeTool.FindTarget(inputSource, out hitPoint);
			if (nearTarget != null)
			{
				IsNearObject = true;
				return nearTarget;
			}

			hitPoint = null;
			IsNearObject = false;
			inputSource.Ray.Color = new Color(1, 1, 1);

			Dictionary RayIntersectionResult = inputSource.IntersectRay();

			if (RayIntersectionResult.Count > 0)
			{
				hitPoint = (Vector3)RayIntersectionResult["position"];

				for (var node = (Spatial)RayIntersectionResult["collider"]; node != null; node = node.GetParent() as Spatial)
				{
					var hitPointNormal = (Vector3)RayIntersectionResult["normal"];
					inputSource.HitPoint = (Vector3)hitPoint;
					inputSource.HitPointNormal = hitPointNormal;
					if (node.GetChild<TargetBehavior>() != null)
					{
						return node;
					}
				}

				inputSource.HitPoint = inputSource.GlobalTransform.origin - inputSource.GlobalTransform.basis.z.Normalized() * 1.5f;
				inputSource.HitPointNormal = inputSource.GlobalTransform.basis.z;
			}
			else
			{
				inputSource.HitPoint = inputSource.GlobalTransform.origin - inputSource.GlobalTransform.basis.z.Normalized() * 1.5f;
				inputSource.HitPointNormal = inputSource.GlobalTransform.basis.z;
			}

			return null;
		}
	}
}
