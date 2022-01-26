using System;
using Assets.Scripts.User;
using Assets.Scripts.Behaviors;
using MixedRealityExtension.Util.GodotHelper;
using Godot;
using Microsoft.MixedReality.Toolkit.Input;

namespace Assets.Scripts.Tools
{
	public enum GrabState
	{
		Grabbed,
		Released
	}

	public class GrabStateChangedArgs
	{
		public GrabState OldGrabState { get; }
		public GrabState NewGrabState { get; }
		public InputSource InputSource { get; }

		public GrabStateChangedArgs(GrabState oldGrabState, GrabState newGrabState, InputSource inputSource)
		{
			OldGrabState = oldGrabState;
			NewGrabState = newGrabState;
			InputSource = inputSource;
		}
	}

	public class GrabTool : Tool
	{
		readonly RID shape = PhysicsServer.ShapeCreate(PhysicsServer.ShapeType.Sphere);
		readonly PhysicsShapeQueryParameters shapeQueryParameters;
		PhysicsDirectSpaceState spaceState;

		// Smoothing factor for query detection. If an object is detected in the query, the queried radius then becomes queryRadius * (1 + querySmoothingFactor) to reduce the sensitivity.
		private float querySmoothingFactor = 0.4f;
		private float queryRadius = 0.05f;
		private bool currentGrabbable;
		private Spatial currentGrabbableActor;

		// The distance between the grabbable target and the grab tool.
		private Vector3 grabbableOffset = Vector3.Zero;

		public bool GrabActive { get; private set; } = false;
		public EventHandler<GrabStateChangedArgs> GrabStateChanged { get; set; }

		public GrabTool()
		{
			shapeQueryParameters = new PhysicsShapeQueryParameters()
			{
				CollideWithAreas = true,
				CollideWithBodies = true,
				ShapeRid = shape,
				Margin = 0.04f
			};
		}

		public Vector3 GetNearGraspPoint(InputSource inputSource)
		{
			return inputSource.IndexTip.GlobalTransform.origin;
		}

		internal Spatial FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			float radius;
			hitPoint = GetNearGraspPoint(inputSource);

			if (currentGrabbable)
			{
				radius = queryRadius * (1 + querySmoothingFactor);
			}
			else
			{
				radius = queryRadius;
			}
			currentGrabbable = false;

			PhysicsServer.ShapeSetData(shape, radius);
			shapeQueryParameters.Transform = new Transform(Basis.Identity, (Vector3)hitPoint);
			spaceState = inputSource.GetWorld().DirectSpaceState;
			var intersectShapes = spaceState.IntersectShape(shapeQueryParameters);

			var intersections = spaceState.GetRestInfo(shapeQueryParameters);
			if (intersections.Count != 0)
			{
				var collider = GD.InstanceFromId((ulong)(int)intersections["collider_id"]) as Spatial;

				Spatial actor = collider;
				TargetBehavior behavior = null;
				while (actor != null && behavior == null)
				{
					actor = actor.GetParent() as Spatial;
					behavior = actor?.GetChild<TargetBehavior>();
				}
				if (behavior == null || actor == null) return null;

				if (behavior.Grabbable)
				{
					currentGrabbable = behavior.Grabbable;
					currentGrabbableActor = actor;

					inputSource.HandRayHitPoint = (Vector3)intersections["point"];
					inputSource.SetCursorNormal((Vector3)intersections["normal"]);
					inputSource.SetHandRayColor(new Color(0.12f, 0.92f, 0.12f));
					return currentGrabbableActor;
				}
			}

			currentGrabbableActor = null;
			return null;
		}

		protected override void UpdateTool(InputSource inputSource)
		{
			if (currentGrabbableActor != null)
			{
				if (inputSource.PinchChaged)
				{
					if (inputSource.IsPinching)
					{
						if (!GrabActive)
						{
							GrabActive = true;

							var grabBehavior = currentGrabbableActor.GetBehavior<TargetBehavior>();
							if (grabBehavior != null)
							{
								var mwUser = grabBehavior.GetMWUnityUser(inputSource.UserNode);
								if (mwUser != null)
								{
									grabBehavior.Context.StartGrab(mwUser);
									grabBehavior.IsGrabbed = true;
								}
							}

							var nearGraspPoint = GetNearGraspPoint(inputSource);
							var eventData = new MixedRealityPointerEventData(this, nearGraspPoint);
							grabbableOffset = currentGrabbableActor.GlobalTransform.origin - nearGraspPoint;

							currentGrabbableActor.HandleEvent<IMixedRealityPointerHandler>(nameof(IMixedRealityPointerHandler.OnPointerDown), eventData);
							GrabStateChanged?.Invoke(this, new GrabStateChangedArgs(GrabState.Released, GrabState.Grabbed, inputSource));
						}
					}
					else
					{
						GrabActive = false;
						var grabBehavior = currentGrabbableActor.GetBehavior<TargetBehavior>();
						if (grabBehavior != null)
						{
							var mwUser = grabBehavior.GetMWUnityUser(inputSource.UserNode);
							if (mwUser != null)
							{
								grabBehavior.Context.EndGrab(mwUser);
								grabBehavior.IsGrabbed = false;
							}
						}

						var eventData = new MixedRealityPointerEventData(this, GetNearGraspPoint(inputSource));
						currentGrabbableActor.HandleEvent<IMixedRealityPointerHandler>(nameof(IMixedRealityPointerHandler.OnPointerUp), eventData);
						GrabStateChanged?.Invoke(this, new GrabStateChangedArgs(GrabState.Grabbed, GrabState.Released, inputSource));

						currentGrabbableActor = null;
						grabbableOffset = Vector3.Zero;
					}
				}
				else if (GrabActive)
				{
					var nearGraspPoint = GetNearGraspPoint(inputSource);
					var eventData = new MixedRealityPointerEventData(this, nearGraspPoint);
					currentGrabbableActor.GlobalTransform = new Transform(currentGrabbableActor.GlobalTransform.basis, nearGraspPoint + grabbableOffset);
					currentGrabbableActor.HandleEvent<IMixedRealityPointerHandler>(nameof(IMixedRealityPointerHandler.OnPointerDragged), eventData);

					inputSource.HandRayHitPoint = inputSource.HandRayOrigin;
				}

			}
		}

		public override void CleanUp() { }
	}
}
