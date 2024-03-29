// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.API;
using MixedRealityExtension.Core.Collision;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.GodotHelper;
using System;
using Godot;

using MREContactPoint = MixedRealityExtension.Core.Collision.ContactPoint;


namespace MixedRealityExtension.Core
{
	/// <summary>
	/// The type of the collider.
	/// </summary>
	public enum ColliderType
	{
		/// <summary>
		/// No collider.
		/// </summary>
		None = 0,

		/// <summary>
		/// Choose best collider shape for mesh
		/// </summary>
		Auto,

		/// <summary>
		/// Box shaped collider.
		/// </summary>
		Box,

		/// <summary>
		/// Sphere shaped collider.
		/// </summary>
		Sphere,

		/// <summary>
		/// Capsule shaped collider.
		/// </summary>
		Capsule,

		/// <summary>
		/// Cylinder shaped collider.
		/// </summary>
		Cylinder,

		/// <summary>
		/// Mesh collider.
		/// </summary>
		Mesh
	}

	internal partial class Collider : Area3D, ICollider
	{
		private static readonly Variant colliderScript = new Collider().GetScript();

		public static Collider Instantiate(Area3D area)
		{
			var parent = area.GetParent();
			ulong objId = area.GetInstanceId();

			area.SetScript(colliderScript);
			var newArea3D = GD.InstanceFromId(objId) as Collider;
			newArea3D.SetProcess(true);
			return newArea3D;
		}

		private CollisionShape3D _collider;
		private Actor _ownerActor;
		private ColliderEventType _colliderEventSubscriptions = ColliderEventType.None;
		private Godot.PhysicsBody3D _physicsbody; // rigidbody or staticbody

		/// <inheritdoc />
		public bool IsEnabled => !_collider.Disabled;

		/// <inheritdoc />
		public bool IsTrigger => _physicsbody?.GetChild<CollisionShape3D>() == null ? true : false;

		/// <inheritdoc />
		public float Bounciness => _physicsbody is Godot.RigidBody3D rigidBody
				? rigidBody.PhysicsMaterialOverride.Bounce : ((StaticBody3D)_physicsbody).PhysicsMaterialOverride.Bounce;

		/// <inheritdoc />
		public float StaticFriction => DynamicFriction;

		/// <inheritdoc />
		public float DynamicFriction => _physicsbody is Godot.RigidBody3D rigidBody
				? rigidBody.PhysicsMaterialOverride.Friction : ((StaticBody3D)_physicsbody).PhysicsMaterialOverride.Friction;

		// /// <inheritdoc />
		//public CollisionLayer CollisionLayer { get; set; }

		/// <inheritdoc />
		public ColliderType Shape { get; private set; }

		internal void Initialize(CollisionShape3D collisionShape, ColliderType? shape = null)
		{
			_ownerActor = collisionShape.GetParent().GetParent<Actor>()
				?? throw new Exception("An MRE collider must be associated with an MRE actor.");
			if (_physicsbody == null)
			{
				_physicsbody = _ownerActor.GetParent() as Godot.RigidBody3D;
				if (_physicsbody == null)
				{
					_physicsbody = new StaticBody3D()
					{
						Name = "StaticBody3D",
						PhysicsMaterialOverride = new PhysicsMaterial()
					};
					_ownerActor.AddChild(_physicsbody);
				}
				_physicsbody.AddChild(collisionShape.Duplicate());
			}
			_collider = collisionShape;

			if (shape.HasValue)
			{
				Shape = shape.Value;
			}
			else if (collisionShape.Shape is SphereShape3D)
			{
				Shape = ColliderType.Sphere;
			}
			else if (collisionShape.Shape is BoxShape3D)
			{
				Shape = ColliderType.Box;
			}
			else if (collisionShape.Shape is CapsuleShape3D)
			{
				Shape = ColliderType.Capsule;
			}
			else if (collisionShape.Shape is ConcavePolygonShape3D)
			{
				Shape = ColliderType.Mesh;
			}
		}

		internal void ApplyPatch(ColliderPatch patch)
		{
			_collider.Disabled = _collider.Disabled.GetPatchApplied(!IsEnabled.ApplyPatch(patch.Enabled));
			var physicsBodyCollisionShape3D = _physicsbody?.GetChild<CollisionShape3D>();
			if (physicsBodyCollisionShape3D != null)
				physicsBodyCollisionShape3D.Disabled = _collider.Disabled;

			if (patch.IsTrigger ?? false)
			{
				var rigidbodyShape = _physicsbody.GetChild<CollisionShape3D>();
				if (rigidbodyShape != null)
					_physicsbody.RemoveChild(rigidbodyShape);
			}

			if (patch.StaticFriction.HasValue)
				GD.PushWarning("StaticFriction is not supported in godot mre. please use DynamicFriction instead.");

			if (_physicsbody is Godot.RigidBody3D rigidBody)
			{
				rigidBody.PhysicsMaterialOverride.Bounce = rigidBody.PhysicsMaterialOverride.Bounce.GetPatchApplied(Bounciness.ApplyPatch(patch.Bounciness));
				rigidBody.PhysicsMaterialOverride.Friction = rigidBody.PhysicsMaterialOverride.Friction.GetPatchApplied(DynamicFriction.ApplyPatch(patch.DynamicFriction));
			}
			if (_physicsbody is Godot.StaticBody3D staticBody)
			{
				staticBody.PhysicsMaterialOverride.Bounce = staticBody.PhysicsMaterialOverride.Bounce.GetPatchApplied(Bounciness.ApplyPatch(patch.Bounciness));
				staticBody.PhysicsMaterialOverride.Friction = staticBody.PhysicsMaterialOverride.Friction.GetPatchApplied(DynamicFriction.ApplyPatch(patch.DynamicFriction));
			}
			MREAPI.AppsAPI.LayerApplicator.ApplyLayerToCollider(patch.Layer, this);
			MREAPI.AppsAPI.LayerApplicator.ApplyLayerToCollider(patch.Layer, _physicsbody);

			if (patch.EventSubscriptions != null)
			{
				// Clear existing subscription flags and set them to the new values.  We do not patch arrays,
				// and thus we will always send the entire value down for all of the subscriptions.
				_colliderEventSubscriptions = ColliderEventType.None;
				foreach (var sub in patch.EventSubscriptions)
				{
					_colliderEventSubscriptions |= sub;
					if (sub == ColliderEventType.CollisionEnter || sub == ColliderEventType.CollisionExit)
					{
						if (_physicsbody is Godot.RigidBody3D rigid)
						{
							rigid.ContactMonitor = true;
							rigid.MaxContactsReported = 32;
							if (sub == ColliderEventType.CollisionEnter)
								rigid.Connect("body_shape_entered", new Callable(this, nameof(OnBodyShapeEnter)));
							else if (sub == ColliderEventType.CollisionExit)
								rigid.Connect("body_shape_exited", new Callable(this, nameof(OnBodyShapeExit)));
						}
					}
					else if (sub == ColliderEventType.TriggerEnter)
					{
						Connect("area_entered", new Callable(this, nameof(OnAreaEnter)));
					}
					else if (sub == ColliderEventType.TriggerExit)
					{
						Connect("area_exited", new Callable(this, nameof(OnAreaExit)));
					}
				}
			}
		}

		internal void SynchronizeEngine(ColliderPatch patch)
		{
			ApplyPatch(patch);
		}

		internal ColliderPatch GenerateInitialPatch()
		{
			ColliderGeometry colliderGeo = null;

			// Note: SDK has no "mesh" collider type
			if (Shape == ColliderType.Auto || Shape == ColliderType.Mesh)
			{
				colliderGeo = new AutoColliderGeometry();
			}
			else if (_collider.Shape is SphereShape3D sphereCollider)
			{
				colliderGeo = new SphereColliderGeometry()
				{
					Radius = sphereCollider.Radius,
					Center = _collider.Transform.origin.CreateMWVector3()
				};
			}
			else if (_collider.Shape is BoxShape3D boxCollider)
			{
				colliderGeo = new BoxColliderGeometry()
				{
					Size = (boxCollider.Size * 0.999998f).CreateMWVector3(),
					Center = _collider.Transform.origin.CreateMWVector3()
				};
			}
			else if (_collider.Shape is CapsuleShape3D capsuleCollider)
			{
				// The size vector describes the dimensions of the bounding box containing the collider
				MWVector3 size;
				/*FXIME there is no direction in godot CapsuleShape3D.
				if (capsuleCollider.direction == 0)
				{
					size = new MWVector3(capsuleCollider.Height, 2 * capsuleCollider.Radius, 2 * capsuleCollider.Radius);
				}
				else if (capsuleCollider.direction == 1)
				{
					size = new MWVector3(2 * capsuleCollider.Radius, capsuleCollider.Height, 2 * capsuleCollider.Radius);
				}
				else
				*/
				{
					size = new MWVector3(2 * capsuleCollider.Radius, 2 * capsuleCollider.Radius, capsuleCollider.Height);
				}

				colliderGeo = new CapsuleColliderGeometry()
				{
					Center = _collider.Transform.origin.CreateMWVector3(),
					Size = size
				};
			}
			else
			{
				_ownerActor.App.Logger.LogWarning($"MRE SDK does not support the following Godot collision shape and will not " +
					$"be available in the MRE app.  Collider Type: {_collider.GetType()}");
			}

			return colliderGeo == null ? null : new ColliderPatch()
			{
				Enabled = !_collider.Disabled,
				IsTrigger = IsTrigger,
				Bounciness = Bounciness,
				StaticFriction = StaticFriction,
				DynamicFriction = DynamicFriction,
				Layer = MREAPI.AppsAPI.LayerApplicator.DetermineLayerOfCollider(this),

				Geometry = colliderGeo
			};
		}

		private void OnAreaEnter(Area3D area)
		{
			if (area is Collider collider && collider._physicsbody is Godot.RigidBody3D)
			{
				SendAreaEvent(ColliderEventType.TriggerEnter, area);
			}
		}

		private void OnAreaExit(Area3D area)
		{
			if (area is Collider collider && collider._physicsbody is Godot.RigidBody3D)
			{
				SendAreaEvent(ColliderEventType.TriggerExit, area);
			}
		}

		private void OnBodyShapeEnter(int bodyId, CollisionObject3D body, int bodyShape, int areaShape)
		{
			SendBodyEvent(ColliderEventType.CollisionEnter, bodyId, body, areaShape);
		}

		private void OnBodyShapeExit(int bodyId, CollisionObject3D body, int bodyShape, int areaShape)
		{
			SendBodyEvent(ColliderEventType.CollisionExit, bodyId, body, areaShape);
		}

		private void SendAreaEvent(ColliderEventType eventType, Area3D area)
		{
			/* temporary comment
			if (!_ownerActor.App.IsAuthoritativePeer && !_ownerActor.IsGrabbed)
			{
				return;
			}
			*/
			var otherActor = area.GetParent<Actor>();
			if (otherActor != null && otherActor.App.InstanceId == _ownerActor.App.InstanceId)
			{
				_ownerActor.App.EventManager.QueueEvent(
					new TriggerEvent(_ownerActor.Id, eventType, otherActor.Id));
			}
		}

		private void SendBodyEvent(ColliderEventType eventType, int bodyId, CollisionObject3D body, int areaShape)
		{
			if (!_ownerActor.App.IsAuthoritativePeer && !_ownerActor.IsGrabbed)
			{
				return;
			}

			Actor otherActor = null;
			if (body is Godot.RigidBody3D rigidBody)
			{
				otherActor = rigidBody.GetChild<Actor>();
			}
			else if (body is Godot.StaticBody3D staticBody)
			{
				otherActor = staticBody.GetParent<Actor>();
			}

			if (otherActor != null && otherActor.App.InstanceId == _ownerActor.App.InstanceId)
			{
				var sceneRoot = _ownerActor.App.SceneRoot;
				var state = PhysicsServer3D.BodyGetDirectState(_physicsbody.GetRid());
				MREContactPoint[] contacts = new MREContactPoint[state.GetContactCount()];
				Vector3 totalImpulse = Vector3.Zero;
				int contactObjectIndex = 0;

				for (int i = 0; i < contacts.Length; i++)
				{
					var ColliderObject = (Node3D)state.GetContactColliderObject(i);
					contacts[i] = new MREContactPoint()
					{
						Normal = sceneRoot.ToLocal(ColliderObject.ToGlobal(state.GetContactLocalNormal(i))).CreateMWVector3(),
						Point = sceneRoot.ToLocal(state.GetContactColliderPosition(i)).CreateMWVector3(),
						Separation = 0 // not support
					};
					totalImpulse += ColliderObject.ToGlobal(state.GetContactLocalNormal(i)) * state.GetContactImpulse(i);
					if (bodyId == (int)state.GetContactColliderId(0))
						contactObjectIndex = i;
				}

				var collisionData = new CollisionData()
				{
					otherActorId = otherActor.Id,
					Contacts = contacts,
					Impulse = sceneRoot.ToLocal(totalImpulse).CreateMWVector3(),
					RelativeVelocity = state.GetContactColliderVelocityAtPosition(contactObjectIndex).CreateMWVector3()
				};

				_ownerActor.App.EventManager.QueueEvent(
					new CollisionEvent(_ownerActor.Id, eventType, collisionData));
			}
		}
	}
}
