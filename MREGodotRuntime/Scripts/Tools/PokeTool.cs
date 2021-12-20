using Assets.Scripts.Behaviors;
using Assets.Scripts.User;
using Godot;
using Godot.Collections;
using MixedRealityExtension.Core;
using Microsoft.MixedReality.Toolkit.Input;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Util.GodotHelper;

namespace Assets.Scripts.Tools
{
	public class PokeTool : Tool
	{
		RID shape = PhysicsServer.ShapeCreate(PhysicsServer.ShapeType.Sphere);
		PhysicsDirectSpaceState spaceState;
		PhysicsShapeQueryParameters shapeQueryParameters;

		public ITouchableBase ClosestProximityTouchable { get; private set; }
		public float TouchableDistance { get; } = 0.2f;
		public Spatial CurrentTouchableObjectDown { get; private set; }
		public Vector3 PreviousPosition { get; private set; } = Vector3.Zero;

		private Spatial CurrentPointerTarget;
		private Vector3 RayStartPoint;
		private Vector3 RayEndPoint;
		private float previousClosestDistance = 0.0f;
		private Vector3 hitPointNormal;

		public PokeTool()
		{
			PhysicsServer.ShapeSetData(shape, TouchableDistance);
			shapeQueryParameters = new PhysicsShapeQueryParameters()
			{
				CollideWithAreas = true,
				CollideWithBodies = true,
				ShapeRid = shape,
				Margin = 0.04f
			};
		}

		internal Spatial FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			spaceState = inputSource.GetWorld().DirectSpaceState;
			shapeQueryParameters.Transform = inputSource.PokePointer.GlobalTransform;
			var intersectShapes = spaceState.IntersectShape(shapeQueryParameters);

			hitPoint = null;

			ITouchableBase newClosestTouchable = null;
			var closestDistance = float.PositiveInfinity;
			Vector3 closestNormal = -inputSource.PokePointer.GlobalTransform.basis.z.Normalized();
			foreach (Dictionary intersectResult in intersectShapes)
			{
				var collider = (Spatial)intersectResult["collider"];

				Spatial actor = collider;
				ITouchableBase touchable = null;
				while (touchable == null)
				{
					actor = actor?.GetParent() as Spatial;
					if (actor == null) break;
					touchable = actor.GetChild<ITouchableBase>();
				}
				if (touchable == null || actor == null) continue;

				float distance = touchable.DistanceToTouchable(inputSource.PokePointer.GlobalTransform.origin, out closestNormal);

				if (distance < closestDistance)
				{
					newClosestTouchable = touchable;
					closestDistance = distance;
					previousClosestDistance = distance;
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
				RayStartPoint = inputSource.PokePointer.GlobalTransform.origin + touchableVector;
				Vector3 to = inputSource.PokePointer.GlobalTransform.origin - touchableVector;
				var IntersectRayResult = spaceState.IntersectRay(RayStartPoint, to, collideWithAreas: true);
				if (IntersectRayResult.Count > 0)
				{
					Vector3 rayEndPoint = (Vector3)IntersectRayResult["position"];
					var collider = (Spatial)IntersectRayResult["collider"];
					hitPointNormal = (Vector3)IntersectRayResult["normal"];
					Spatial actor = collider;
					ITouchableBase touchable = null;
					while (touchable == null)
					{
						actor = actor?.GetParent() as Spatial;
						if (actor == null) break;
						touchable = actor.GetChild<ITouchableBase>();
					}
					if (touchable == null || actor == null) return null;

					CurrentPointerTarget = actor;
					hitPoint = rayEndPoint;
					RayEndPoint = rayEndPoint;

					if (CurrentTouchableObjectDown == null)
					{
						inputSource.SetCursorNormal(hitPointNormal);
						inputSource.HandRayHitPoint = (Vector3)hitPoint;
					}

					return CurrentPointerTarget;
				}
			}

			return null;
		}

		protected override void UpdateTool(InputSource inputSource)
		{
			Position = inputSource.PokePointer.GlobalTransform.origin;
			if (CurrentPointerTarget != null && ClosestProximityTouchable != null)
			{
				float distToTouchable = RayStartPoint.DistanceTo(RayEndPoint) - TouchableDistance;
				bool newIsDown = distToTouchable < ClosestProximityTouchable.DebounceThreshold;
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

			PreviousPosition = inputSource.PokePointer.GlobalTransform.origin;
		}

		public override void CleanUp() { }

		private void TryRaisePokeDown(InputSource inputSource)
		{
			if (CurrentTouchableObjectDown == null && previousClosestDistance > 0.0f)
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

							var eventData = new TouchInputEventData(this, Position);
							CurrentTouchableObjectDown.HandleEvent<IMixedRealityTouchHandler>(nameof(IMixedRealityTouchHandler.OnTouchStarted), eventData);
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

						var eventData = new TouchInputEventData(this, Position);
						CurrentTouchableObjectDown.HandleEvent<IMixedRealityTouchHandler>(nameof(IMixedRealityTouchHandler.OnTouchCompleted), eventData);
					}
				}
				CurrentTouchableObjectDown = null;
			}
		}

		private void RaiseTouchUpdapted(InputSource inputSource)
		{
			if (CurrentTouchableObjectDown != null)
			{
				var pokePointerOrigin = inputSource.PokePointer.GlobalTransform.origin;
				var eventData = new TouchInputEventData(this, pokePointerOrigin);

				inputSource.HandRayHitPoint = pokePointerOrigin;
				inputSource.SetCursorNormal(hitPointNormal);
				CurrentTouchableObjectDown.HandleEvent<IMixedRealityTouchHandler>(nameof(IMixedRealityTouchHandler.OnTouchUpdated), eventData);
			}
		}

		internal void OnTargetChanged(InputSource inputSource)
		{
			TryRaisePokeUp(inputSource);
		}

		private bool IsObjectPartOfTouchable(Spatial targetObject, ITouchableBase touchable)
		{
			return targetObject != null && touchable != null && targetObject.FindNode(((Spatial)touchable).Name, owned: false) != null;
		}
	}
}
