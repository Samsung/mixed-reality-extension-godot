using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using Godot;
using Godot.Collections;
using MixedRealityExtension.Core;
using Microsoft.MixedReality.Toolkit.Input;

namespace Assets.Scripts.Tools
{
	public class PokeTool : Tool
	{
		RID shape = PhysicsServer.ShapeCreate(PhysicsServer.ShapeType.Sphere);
		PhysicsDirectSpaceState spaceState;
		PhysicsShapeQueryParameters shapeQueryParameters;

		public BaseNearInteractionTouchable ClosestProximityTouchable { get; private set; }
		public float TouchableDistance { get; } = 0.2f;
		public Spatial CurrentTouchableObjectDown { get; private set; }
		public Vector3 PreviousPosition { get; private set; } = Vector3.Zero;
		public Vector3 IntersectionPosition { get; private set; }

		private Spatial CurrentPointerTarget;
		private Vector3 RayStartPoint;
		private Vector3 RayEndPoint;

		public PokeTool()
		{
			PhysicsServer.ShapeSetData(shape, 0.01f);
			shapeQueryParameters = new PhysicsShapeQueryParameters()
			{
				CollideWithAreas = true,
				CollideWithBodies = true,
				ShapeRid = shape,
				Margin = 0.02f
			};
		}

		internal Spatial FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			spaceState = inputSource.GetWorld().DirectSpaceState;
			shapeQueryParameters.Transform = inputSource.PokePointer.GlobalTransform;
			var intersectShapes = spaceState.IntersectShape(shapeQueryParameters);

			BaseNearInteractionTouchable touchable = null;
			Spatial touchableActor = null;
			hitPoint = null;

			foreach (Dictionary intersectResult in intersectShapes)
			{
				var closestDistance = float.PositiveInfinity;
				var collider = (Spatial)intersectResult["collider"];

				for (touchableActor = collider; touchableActor != null; touchableActor = touchableActor.GetParent<Spatial>())
					if (touchableActor is Actor) break;

				if (touchableActor == null || !((Actor)touchableActor).Touchable)
					return null;

				touchable = FindBaseNearInteractionTouchable(touchableActor);

				BaseNearInteractionTouchable FindBaseNearInteractionTouchable(Node node)
				{
					if (node is BaseNearInteractionTouchable baseNearInteractionTouchable)
						return baseNearInteractionTouchable;

					foreach (Node child in node.GetChildren())
					{
						var touchableChild = FindBaseNearInteractionTouchable(child);
						if (touchableChild != null) return touchableChild;
					}
					return null;
				}

				if (touchable != null)
				{
					float distance = touchable.DistanceToTouchable(inputSource.PokePointer.GlobalTransform.origin, out Vector3 normal);
					if (distance < closestDistance)
					{
						ClosestProximityTouchable = touchable;
						closestDistance = distance;
					}
					break;
				}
			}

			if (CurrentTouchableObjectDown != null)
			{
				if (!IsObjectPartOfTouchable(CurrentTouchableObjectDown, touchable))
				{
					TryRaisePokeUp(inputSource);
				}
			}

			ClosestProximityTouchable = touchable;

			RayStartPoint = inputSource.PokePointer.GlobalTransform.origin + inputSource.PokePointer.GlobalTransform.basis.z.Normalized() * TouchableDistance;
			Vector3 to = -inputSource.PokePointer.GlobalTransform.basis.z.Normalized() * 1.5f;
			var IntersectRayResult = spaceState.IntersectRay(RayStartPoint, to, collideWithAreas: true);
			if (IntersectRayResult.Count > 0)
			{
				Vector3 rayEndPoint = (Vector3)IntersectRayResult["position"];
				var collider = (Spatial)IntersectRayResult["collider"];
				Spatial actor;

				for (actor = collider; actor != null; actor = actor.GetParent<Spatial>())
					if (actor is Actor) break;

				if (actor == null || !((Actor)actor).Touchable)
					return null;

				CurrentPointerTarget = actor;
				IntersectionPosition = inputSource.PokePointer.GlobalTransform.origin - inputSource.PokePointer.GlobalTransform.basis.z.Normalized() * 0.01f;
				hitPoint = IntersectionPosition;
				RayEndPoint = rayEndPoint;
			}

			return touchableActor;
		}

		protected override void UpdateTool(InputSource inputSource)
		{
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

			PreviousPosition = RayStartPoint;
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
							buttonBehavior.Context.StartButton(mwUser, IntersectionPosition);
							((SpatialMaterial)inputSource.CollisionPoint.MaterialOverride).AlbedoColor = new Color(1, 0, 0);
						}
					}

					if (ClosestProximityTouchable.node is IMixedRealityTouchHandler handler)
						handler.OnTouchStarted(new HandTrackingInputEventData(this));
				}
			}
			else
			{
				RaiseTouchUpdapted();
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
						buttonBehavior.Context.EndButton(mwUser, IntersectionPosition);
						buttonBehavior.Context.Click(mwUser, IntersectionPosition);
					}
				}

				if (ClosestProximityTouchable.node is IMixedRealityTouchHandler handler)
					handler.OnTouchCompleted(new HandTrackingInputEventData(this));
				CurrentTouchableObjectDown = null;
			}
		}

		private void RaiseTouchUpdapted()
		{
			if (CurrentTouchableObjectDown != null)
			{
				if (ClosestProximityTouchable.node is IMixedRealityTouchHandler handler)
					handler.OnTouchUpdated(new HandTrackingInputEventData(this));
			}
		}

		internal void OnTargetChanged(InputSource inputSource)
		{
			TryRaisePokeUp(inputSource);
		}

		private bool IsObjectPartOfTouchable(Spatial targetObject, BaseNearInteractionTouchable touchable)
		{
			return targetObject != null && touchable != null && targetObject.FindNode(touchable.Name, owned: false) != null;
		}
	}
}
