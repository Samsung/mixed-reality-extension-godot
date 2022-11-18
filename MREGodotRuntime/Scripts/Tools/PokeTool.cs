using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using Godot;
using Godot.Collections;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Util.GodotHelper;
using System;

namespace Assets.Scripts.Tools
{
	public class PokeTool : Tool
	{
		RID shape = PhysicsServer3D.SphereShapeCreate();
		PhysicsDirectSpaceState3D spaceState;
		PhysicsShapeQueryParameters3D shapeQueryParameters;

		public ITouchableBase ClosestProximityTouchable { get; private set; }
		public float TouchableDistance { get; } = 0.02f;
		public Node3D CurrentTouchableObjectDown { get; private set; }
		public Vector3 PreviousPosition { get; private set; } = Vector3.Zero;

		private Node3D CurrentPointerTarget;
		private Vector3 RayStartPoint;
		private Vector3 RayEndPoint;
		private Vector3 hitPointNormal;

		public PokeTool()
		{
			PhysicsServer3D.ShapeSetData(shape, TouchableDistance);
			shapeQueryParameters = new PhysicsShapeQueryParameters3D()
			{
				CollideWithAreas = true,
				CollideWithBodies = true,
				ShapeRid = shape,
				Margin = 0.04f
			};
		}

		internal Node3D FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			spaceState = inputSource.GetWorld3d().DirectSpaceState;
			shapeQueryParameters.Transform = inputSource.GlobalTransform;
			var intersectShapes = spaceState.IntersectShape(shapeQueryParameters);

			hitPoint = null;

			ITouchableBase newClosestTouchable = null;
			var closestDistance = float.PositiveInfinity;
			Vector3 closestNormal = -inputSource.GlobalTransform.basis.z.Normalized();
			foreach (Dictionary intersectResult in intersectShapes)
			{
				var collider = (Node3D)intersectResult["collider"];
				Vector3 normal;

				Node3D actor = collider;
				ITouchableBase touchable = null;
				while (touchable == null)
				{
					actor = actor?.GetParent() as Node3D;
					if (actor == null) break;
					touchable = actor.GetChild<ITouchableBase>();
				}
				if (touchable == null || actor == null) continue;
				float distance = touchable.DistanceToTouchable(inputSource.GlobalTransform.origin, out normal);

				bool bothInside = (distance <= 0f) && (closestDistance <= 0f);
				bool betterFit = bothInside ? Mathf.Abs(distance) < Mathf.Abs(closestDistance) : distance < closestDistance;
				if (betterFit)
				{
					newClosestTouchable = touchable;
					closestDistance = distance;
					closestNormal = normal;
				}
			}

			if (CurrentTouchableObjectDown != null)
			{
				if (!IsObjectPartOfTouchable(CurrentTouchableObjectDown, newClosestTouchable))
				{
					TryRaisePokeUp(inputSource);
				}
			}

			ClosestProximityTouchable = newClosestTouchable;

			if (newClosestTouchable != null)
			{
				var touchableVector = closestNormal * TouchableDistance;
				RayStartPoint = inputSource.GlobalTransform.origin + touchableVector;
				Vector3 to = inputSource.GlobalTransform.origin - touchableVector;
				var IntersectRayResult = spaceState.IntersectRay(new PhysicsRayQueryParameters3D() {
					From = RayStartPoint,
					To = to,
					CollideWithBodies = true,
					CollideWithAreas = true});
				if (IntersectRayResult.Count > 0)
				{
					Vector3 rayEndPoint = (Vector3)IntersectRayResult["position"];
					var collider = (Node3D)IntersectRayResult["collider"];
					hitPointNormal = (Vector3)IntersectRayResult["normal"];
					Node3D actor = collider;
					ITouchableBase touchable = null;
					while (touchable == null)
					{
						actor = actor?.GetParent() as Node3D;
						if (actor == null) break;
						touchable = actor.GetChild<ITouchableBase>();
					}
					if (touchable == null || actor == null) return null;

					CurrentPointerTarget = actor;
					hitPoint = rayEndPoint;
					RayEndPoint = rayEndPoint;

					if (CurrentTouchableObjectDown == null)
					{
						inputSource.HitPointNormal = hitPointNormal;
						inputSource.HitPoint = (Vector3)hitPoint;
					}

					return CurrentPointerTarget;
				}
			}

			return null;
		}

		protected override void UpdateTool(InputSource inputSource)
		{
			Position = inputSource.GlobalTransform.origin;
			if (CurrentPointerTarget != null && ClosestProximityTouchable != null)
			{
				float distToTouchable = RayStartPoint.DistanceTo(RayEndPoint) - TouchableDistance;
				bool newIsDown = distToTouchable < 0.0f;
				bool newIsUp = distToTouchable > ClosestProximityTouchable.DebounceThreshold;

				if (newIsDown)
				{
					TryRaisePokeDown(inputSource);
				}
				else if (CurrentTouchableObjectDown != null)
				{
					if (newIsUp)
					{
						TryRaisePokeUp(inputSource);
					}
					else
					{
						TryRaisePokeDown(inputSource);
					}
				}
			}

			PreviousPosition = inputSource.GlobalTransform.origin;
		}

		public override void CleanUp() { }

		private void TryRaisePokeDown(InputSource inputSource)
		{
			if (CurrentTouchableObjectDown == null)
			{
				if (IsObjectPartOfTouchable(CurrentPointerTarget, ClosestProximityTouchable))
				{
					CurrentTouchableObjectDown = CurrentPointerTarget;

					var buttonBehavior = CurrentTouchableObjectDown.GetBehavior<ButtonBehavior>();
					if (buttonBehavior != null)
					{
						var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserNode);
						if (mwUser != null)
						{
							buttonBehavior.Context.StartButton(mwUser, Position);

							if (CurrentTouchableObjectDown.HasUserSignal("touch_started"))
								CurrentTouchableObjectDown.EmitSignal("touch_started", inputSource, inputSource.UserNode, Position);
						}
					}
				}
			}
			else
			{
				RaiseTouchUpdapted(inputSource);
			}
		}

		private void TryRaisePokeUp(InputSource inputSource)
		{
			if (CurrentTouchableObjectDown != null)
			{
				var buttonBehavior = CurrentTouchableObjectDown.GetBehavior<ButtonBehavior>();
				if (buttonBehavior != null)
				{
					var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserNode);
					if (mwUser != null)
					{
						buttonBehavior.Context.EndButton(mwUser, Position);
						buttonBehavior.Context.Click(mwUser, Position);

						if (CurrentTouchableObjectDown.HasUserSignal("touch_completed"))
							CurrentTouchableObjectDown.EmitSignal("touch_completed", inputSource, inputSource.UserNode, Position);
					}
				}
				CurrentTouchableObjectDown = null;
			}
		}

		private void RaiseTouchUpdapted(InputSource inputSource)
		{
			if (CurrentTouchableObjectDown != null)
			{
				var buttonBehavior = CurrentTouchableObjectDown.GetBehavior<ButtonBehavior>();
				if (buttonBehavior != null)
				{
					var mwUser = buttonBehavior.GetMWUnityUser(inputSource.UserNode);
					if (mwUser != null)
					{
						if (CurrentTouchableObjectDown.HasUserSignal("touch_updated"))
							CurrentTouchableObjectDown.EmitSignal("touch_updated", inputSource, inputSource.UserNode, Position);
					}
				}

				inputSource.HitPoint = inputSource.GlobalTransform.origin;
				inputSource.HitPointNormal = hitPointNormal;
			}
		}

		internal void OnTargetChanged(InputSource inputSource)
		{
			TryRaisePokeUp(inputSource);
		}

		private bool IsObjectPartOfTouchable(Node3D targetObject, ITouchableBase touchable)
		{
			return targetObject != null && touchable != null && targetObject.FindChild(((Node3D)touchable).Name, owned: false) != null;
		}
	}
}
