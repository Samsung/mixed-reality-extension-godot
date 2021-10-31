// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using System.Linq;
using Godot;
using MixedRealityExtension.Util.GodotHelper;
using Godot.Collections;
using Microsoft.MixedReality.Toolkit.Input;

namespace Assets.Scripts.Tools
{
	public class TargetTool : Tool
	{
		private GrabTool _grabTool = new GrabTool();
		protected PokeTool pokeTool = new PokeTool();
		private TargetBehavior _currentTargetBehavior;

		public bool IsNearObject;

		public Spatial Target { get; private set; }

		public bool TargetGrabbed => _grabTool.GrabActive;

		protected Vector3 CurrentTargetPoint { get; private set; }

		public TargetTool()
		{
			_grabTool.GrabStateChanged += OnGrabStateChanged;
		}

		public override void CleanUp()
		{
			_grabTool.GrabStateChanged -= OnGrabStateChanged;
		}

		public override void OnToolHeld(InputSource inputSource)
		{
			base.OnToolHeld(inputSource);
			pokeTool.OnToolHeld(inputSource);

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
				_grabTool.Update(inputSource, Target);
				if (_grabTool.GrabActive)
				{
					// If a grab is active, nothing should change about the current target.
					return;
				}
			}

			pokeTool.Update(inputSource);
			Vector3? hitPoint;

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
				OnTargetPointUpdated(CurrentTargetPoint);
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

				var handler = IMixedRealityFocusHandler.FindEventHandler<IMixedRealityFocusHandler>(oldTarget);
				handler?.OnFocusExit(new MixedRealityFocusEventData(this, oldTarget, newTarget));
			}

			if (newTarget != null && Godot.Object.IsInstanceValid(newTarget) && !IsNearObject)
			{
				newBehavior.Context.StartTargeting(newBehavior.GetMWUnityUser(inputSource.UserNode), newTargetPoint);

				var handler = IMixedRealityFocusHandler.FindEventHandler<IMixedRealityFocusHandler>(newTarget);
				handler?.OnFocusEnter(new MixedRealityFocusEventData(this, oldTarget, newTarget));
			}

			CurrentTargetPoint = newTargetPoint;
			Target = newTarget;
			_currentTargetBehavior = newBehavior;
		}

		protected virtual void OnTargetPointUpdated(Vector3 point)
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

		private Spatial FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			hitPoint = null;
			Spatial nearTarget = pokeTool.FindTarget(inputSource, out hitPoint);
			if (nearTarget != null)
			{
				IsNearObject = true;
				return nearTarget;
			}

			hitPoint = null;
			IsNearObject = false;

			Dictionary RayIntersectionResult = inputSource.IntersectRay();

			if (RayIntersectionResult.Count > 0)
			{
				hitPoint = (Vector3)RayIntersectionResult["position"];
				var distance = ((Vector3)hitPoint).DistanceTo(inputSource.Hand.GlobalTransform.origin);
				inputSource.RayCastMesh.Scale = new Vector3(1, 1, distance);
				inputSource.RayCastMesh.Translation = new Vector3(0, 0, -distance / 2);

				for (var node = (Spatial)RayIntersectionResult["collider"]; node != null; node = node.GetParent<Spatial>())
				{
					if (node is MixedRealityExtension.Core.Actor a)
					{
						if (node.GetChild<TargetBehavior>() != null)
						{
							var hitPointNormal = (Vector3)RayIntersectionResult["normal"];
							if (!inputSource.CollisionPoint.Visible) inputSource.CollisionPoint.Visible = true;
							var newTransform = LookAtHitPoint((Vector3)hitPoint, hitPointNormal, inputSource.CollisionPoint.GlobalTransform.basis.y);

							if (inputSource.CollisionPoint.GlobalTransform.origin.DistanceSquaredTo((Vector3)hitPoint) > 0.0000001)
							{
								inputSource.CollisionPoint.GlobalTransform = newTransform;
							}
							return node;
						}
						else
						{
							if (inputSource.CollisionPoint.Visible) inputSource.CollisionPoint.Visible = false;
						}
					}
				}
			}
			else
			{
				inputSource.RayCastMesh.Scale = new Vector3(1, 1, 1.5f);
				inputSource.RayCastMesh.Translation = new Vector3(0, 0, -0.75f);
				if (inputSource.CollisionPoint.Visible) inputSource.CollisionPoint.Visible = false;
			}

			return null;
		}

		private Transform LookAtHitPoint(Vector3 hitPoint, Vector3 hitPointNormal, Vector3 up)
		{
			Transform transform = new Transform(Basis.Identity, hitPoint);

			//Y vector
			transform.basis.y = hitPointNormal.Normalized();
			transform.basis.z = -up;
			transform.basis.x = transform.basis.z.Cross(transform.basis.y).Normalized();

			//Recompute z = y cross X
			transform.basis.z = transform.basis.y.Cross(transform.basis.x).Normalized();
			transform.basis = transform.basis.Orthonormalized();
			return transform;
		}

		void OnDestroy()
		{
			_grabTool.Dispose();
		}
	}
}
