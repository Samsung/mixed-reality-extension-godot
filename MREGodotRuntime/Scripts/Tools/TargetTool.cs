// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using System.Linq;
using Godot;
using MixedRealityExtension.Util.GodotHelper;

namespace Assets.Scripts.Tools
{
	public class TargetTool : Tool
	{
		private GrabTool _grabTool = new GrabTool();
		private TargetBehavior _currentTargetBehavior;

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
			if (oldTarget != null && Godot.Object.IsInstanceValid(oldTarget))
			{
				_currentTargetBehavior.Context.EndTargeting(_currentTargetBehavior.GetMWUnityUser(inputSource.UserNode), oldTargetPoint);
			}

			if (newTarget != null && Godot.Object.IsInstanceValid(newTarget))
			{
				newBehavior.Context.StartTargeting(newBehavior.GetMWUnityUser(inputSource.UserNode), newTargetPoint);
			}

			CurrentTargetPoint = newTargetPoint;
			Target = newTarget;
			_currentTargetBehavior = newBehavior;
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
			if (inputSource.rayCast.IsColliding())
			{
				hitPoint = inputSource.rayCast.GetCollisionPoint();
				for (var node = (inputSource.rayCast.GetCollider() as Node); node != null; node = node.GetParent())
				{
					if (node is MixedRealityExtension.Core.Actor a)
					{
						if (node.GetChild<TargetBehavior>() != null)
							return node as Spatial;
					}
				}
			}

			return null;
		}

		void OnDestroy()
		{
			_grabTool.Dispose();
		}
	}
}
