using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using System.Linq;
using Godot;
using Godot.Collections;
using MixedRealityExtension.Util.GodotHelper;
using MixedRealityExtension.Core;
using Microsoft.MixedReality.Toolkit.Input;

namespace Assets.Scripts.Tools
{
	public class PokeTool
	{
		RID shape = PhysicsServer.ShapeCreate(PhysicsServer.ShapeType.Sphere);
		PhysicsDirectSpaceState spaceState;
		PhysicsShapeQueryParameters shapeQueryParameters;

		public BaseNearInteractionTouchable ClosestProximityTouchable { get; private set; }
		public float TouchableDistance { get; } = 0.2f;
		public Spatial CurrentTouchableObjectDown { get; private set; }
		public Vector3 PreviousPosition { get; private set; } = Vector3.Zero;
		public Vector3 Position { get; private set; }

		private Spatial CurrentPointerTarget;

		public PokeTool()
		{
			PhysicsServer.ShapeSetData(shape, 0.01f);
			shapeQueryParameters = new PhysicsShapeQueryParameters()
			{
				CollideWithAreas = true,
				CollideWithBodies = true,
				ShapeRid = shape,
				Margin = 0.04f
			};
		}

		internal Spatial FindTarget(InputSource inputSource)
		{
			spaceState = inputSource.GetWorld().DirectSpaceState;
			shapeQueryParameters.Transform = inputSource.PokePointer.GlobalTransform;
			var intersectShapes = spaceState.IntersectShape(shapeQueryParameters);
			Spatial actor = null;
			BaseNearInteractionTouchable touchable = null;

			foreach (Dictionary intersectResult in intersectShapes)
			{
				var closestDistance = float.PositiveInfinity;
				var collider = (Spatial)intersectResult["collider"];

				for (actor = collider; actor != null; actor = actor.GetParent<Spatial>())
					if (actor is Actor) break;

				touchable = FindBaseNearInteractionTouchable(actor);

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
			return null;
		}

		internal void UpdateTool(InputSource inputSource)
		{
			spaceState = inputSource.GetWorld().DirectSpaceState;
			Vector3 from = inputSource.PokePointer.GlobalTransform.origin + inputSource.PokePointer.GlobalTransform.basis.z.Normalized() * TouchableDistance;
			Vector3 to = -inputSource.PokePointer.GlobalTransform.basis.z.Normalized() * 1.5f;
			var IntersectRayResult = spaceState.IntersectRay(from, to, collideWithAreas: true);
			if (IntersectRayResult.Count > 0 && ClosestProximityTouchable != null)
			{
				Vector3 intersectionPoint = (Vector3)IntersectRayResult["position"];
				var collider = (Spatial)IntersectRayResult["collider"];
				Spatial actor;
				for (actor = collider; actor != null; actor = actor.GetParent<Spatial>())
					if (actor is Actor) break;
				if (actor == null)
					return;
				CurrentPointerTarget = actor;
				float distToTouchable = from.DistanceTo(intersectionPoint) - TouchableDistance;
				Position = inputSource.PokePointer.GlobalTransform.origin - inputSource.PokePointer.GlobalTransform.basis.z.Normalized() * 0.01f;

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

			PreviousPosition = from;
		}

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
						buttonBehavior.Context.EndButton(mwUser, Position);
						buttonBehavior.Context.Click(mwUser, Position);
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
