using System;
using Assets.Scripts.User;
using Assets.Scripts.Behaviors;
using MixedRealityExtension.Util.GodotHelper;
using Godot;

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
		readonly Rid shape = PhysicsServer3D.SphereShapeCreate();
		readonly PhysicsShapeQueryParameters3D shapeQueryParameters;
		PhysicsDirectSpaceState3D spaceState;

		// Smoothing factor for query detection. If an object is detected in the query, the queried radius then becomes queryRadius * (1 + querySmoothingFactor) to reduce the sensitivity.
		private float querySmoothingFactor = 0.4f;
		private float queryRadius = 0.05f;
		private bool currentGrabbable;
		private Node3D currentGrabbableActor;

		// The distance between the grabbable target and the grab tool.
		private Vector3 grabbableOffset = Vector3.Zero;

		public bool GrabActive { get; private set; } = false;
		public EventHandler<GrabStateChangedArgs> GrabStateChanged { get; set; }

		public GrabTool()
		{
			shapeQueryParameters = new PhysicsShapeQueryParameters3D()
			{
				CollideWithAreas = true,
				CollideWithBodies = true,
				ShapeRid = shape,
				Margin = 0.04f
			};
		}

		public Vector3 GetNearGraspPoint(InputSource inputSource)
		{
			return inputSource.GlobalTransform.Origin;
		}

		internal Node3D FindTarget(InputSource inputSource, out Vector3? hitPoint)
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

			PhysicsServer3D.ShapeSetData(shape, radius);
			shapeQueryParameters.Transform = new Transform3D(Basis.Identity, (Vector3)hitPoint);
			spaceState = inputSource.GetWorld3D().DirectSpaceState;
			var intersectShapes = spaceState.IntersectShape(shapeQueryParameters);

			var intersections = spaceState.GetRestInfo(shapeQueryParameters);
			if (intersections.Count != 0)
			{
				var collider = GodotObject.InstanceFromId((ulong)(long)intersections["collider_id"]) as Node3D;

				Node3D actor = collider;
				TargetBehavior behavior = null;
				while (actor != null && behavior == null)
				{
					actor = actor.GetParent() as Node3D;
					behavior = actor?.GetChild<TargetBehavior>();
				}
				if (behavior == null || actor == null) return null;

				if (behavior.Grabbable)
				{
					currentGrabbable = behavior.Grabbable;
					currentGrabbableActor = actor;

					inputSource.HitPoint = (Vector3)intersections["point"];
					inputSource.HitPointNormal = (Vector3)intersections["normal"];
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
							grabbableOffset = currentGrabbableActor.GlobalTransform.Origin - nearGraspPoint;

							if (currentGrabbableActor.HasUserSignal("pointer_down"))
								currentGrabbableActor.EmitSignal("pointer_down", inputSource, inputSource.UserNode, nearGraspPoint);

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

						if (currentGrabbableActor.HasUserSignal("pointer_up"))
							currentGrabbableActor.EmitSignal("pointer_up", inputSource, inputSource.UserNode, GetNearGraspPoint(inputSource));

						GrabStateChanged?.Invoke(this, new GrabStateChangedArgs(GrabState.Grabbed, GrabState.Released, inputSource));

						currentGrabbableActor = null;
						grabbableOffset = Vector3.Zero;
					}
				}
				else if (GrabActive)
				{
					var nearGraspPoint = GetNearGraspPoint(inputSource);
					currentGrabbableActor.GlobalTransform = new Transform3D(currentGrabbableActor.GlobalTransform.Basis, nearGraspPoint + grabbableOffset);

					if (currentGrabbableActor.HasUserSignal("pointer_dragged"))
						currentGrabbableActor.EmitSignal("pointer_dragged", inputSource, inputSource.UserNode, nearGraspPoint);

					inputSource.HitPoint = inputSource.GlobalTransform.Origin;
				}

			}
		}

		public override void CleanUp() { }
	}
}
