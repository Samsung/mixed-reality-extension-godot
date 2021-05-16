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
using System.Linq;
using Godot;

using MREContactPoint = MixedRealityExtension.Core.Collision.ContactPoint;

using GodotCollisionObject = Godot.CollisionObject;
//using UnityCollision = UnityEngine.Collision;

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
		/// Mesh collider.
		/// </summary>
		Mesh
	}

	internal class Collider : Area, ICollider
	{
		//FIXME: need to change path
		private static Resource Resource = ResourceLoader.Load("MREGodotRuntimeLib/Core/Collider.cs");

		public static Collider Instantiate(Area area)
		{
			var parent = area.GetParent();
			ulong objId = area.GetInstanceId();
			parent.RemoveChild(area);

			area.SetScript(Resource);
			var newArea = GD.InstanceFromId(objId) as Collider;
			parent.AddChild(newArea);
			return newArea;
		}

		private CollisionShape _collider;
		private Actor _ownerActor;
		private ColliderEventType _colliderEventSubscriptions = ColliderEventType.None;

		/// <inheritdoc />
		public bool IsEnabled => !_collider.Disabled;
/*FIXME
		/// <inheritdoc />
		public bool IsTrigger => _collider.isTrigger;

		/// <inheritdoc />
		public float Bounciness => _collider.material.bounciness;

		/// <inheritdoc />
		public float StaticFriction => _collider.material.staticFriction;

		/// <inheritdoc />
		public float DynamicFriction => _collider.material.dynamicFriction;
*/
		// /// <inheritdoc />
		//public CollisionLayer CollisionLayer { get; set; }

		/// <inheritdoc />
		public ColliderType Shape { get; private set; }

		internal void Initialize(CollisionShape collisionShape, ColliderType? shape = null)
		{
			_ownerActor = collisionShape.GetParent().GetParent<Actor>()
				?? throw new Exception("An MRE collider must be associated with an MRE actor.");
			_collider = collisionShape;

			if (shape.HasValue)
			{
				Shape = shape.Value;
			}
			else if (collisionShape.Shape is SphereShape)
			{
				Shape = ColliderType.Sphere;
			}
			else if (collisionShape.Shape is BoxShape)
			{
				Shape = ColliderType.Box;
			}
			else if (collisionShape.Shape is CapsuleShape)
			{
				Shape = ColliderType.Capsule;
			}
			/*FIXME
			else if (collisionShape.Shape is MeshCollider)
			{
				Shape = ColliderType.Mesh;
			}
			*/
		}

		internal void ApplyPatch(ColliderPatch patch)
		{
			_collider.Disabled = _collider.Disabled.GetPatchApplied(!IsEnabled.ApplyPatch(patch.Enabled));
			/*FIXME
			_collider.isTrigger = _collider.isTrigger.GetPatchApplied(IsTrigger.ApplyPatch(patch.IsTrigger));
			_collider.material.bounciness = _collider.material.bounciness.GetPatchApplied(Bounciness.ApplyPatch(patch.Bounciness));
			_collider.material.staticFriction = _collider.material.staticFriction.GetPatchApplied(StaticFriction.ApplyPatch(patch.StaticFriction));
			_collider.material.dynamicFriction = _collider.material.dynamicFriction.GetPatchApplied(DynamicFriction.ApplyPatch(patch.DynamicFriction));
			*/

			MREAPI.AppsAPI.LayerApplicator.ApplyLayerToCollider(patch.Layer, this);

			if (patch.EventSubscriptions != null)
			{
				// Clear existing subscription flags and set them to the new values.  We do not patch arrays,
				// and thus we will always send the entire value down for all of the subscriptions.
				_colliderEventSubscriptions = ColliderEventType.None;
				foreach (var sub in patch.EventSubscriptions)
				{
					_colliderEventSubscriptions |= sub;
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
			else if (_collider.Shape is SphereShape sphereCollider)
			{
				colliderGeo = new SphereColliderGeometry()
				{
					Radius = sphereCollider.Radius,
					Center = _collider.Transform.origin.CreateMWVector3()
				};
			}
			else if (_collider.Shape is BoxShape boxCollider)
			{
				colliderGeo = new BoxColliderGeometry()
				{
					Size = boxCollider.Extents.CreateMWVector3(),
					Center = _collider.Transform.origin.CreateMWVector3()
				};
			}
			else if (_collider.Shape is CapsuleShape capsuleCollider)
			{
				// The size vector describes the dimensions of the bounding box containing the collider
				MWVector3 size;
				/*FXIME there is no direction in godot CapsuleShape.
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
				/*FIXME
				IsTrigger = _collider.isTrigger,
				Bounciness = _collider.material.bounciness,
				StaticFriction = _collider.material.staticFriction,
				DynamicFriction = _collider.material.dynamicFriction,
				*/
				Layer = MREAPI.AppsAPI.LayerApplicator.DetermineLayerOfCollider(this),

				Geometry = colliderGeo
			};
		}
/*FIXME
		private void OnTriggerEnter(GodotCollisionObject other)
		{
			if (_colliderEventSubscriptions.HasFlag(ColliderEventType.TriggerEnter))
			{
				SendTriggerEvent(ColliderEventType.TriggerEnter, other);
			}
		}

		private void OnTriggerExit(GodotCollisionObject other)
		{
			if (_colliderEventSubscriptions.HasFlag(ColliderEventType.TriggerExit))
			{
				SendTriggerEvent(ColliderEventType.TriggerExit, other);
			}
		}

		private void OnCollisionEnter(UnityCollision collision)
		{
			if (_colliderEventSubscriptions.HasFlag(ColliderEventType.CollisionEnter))
			{
				SendCollisionEvent(ColliderEventType.CollisionEnter, collision);
			}
		}

		private void OnCollisionExit(UnityCollision collision)
		{
			if (_colliderEventSubscriptions.HasFlag(ColliderEventType.CollisionExit))
			{
				SendCollisionEvent(ColliderEventType.CollisionExit, collision);
			}
		}

		private void SendTriggerEvent(ColliderEventType eventType, GodotCollisionObject otherCollider)
		{
			if (!_ownerActor.App.IsAuthoritativePeer && !_ownerActor.IsGrabbed)
			{
				return;
			}

			var otherActor = otherCollider.gameObject.GetComponent<Actor>();
			if (otherActor != null && otherActor.App.InstanceId == _ownerActor.App.InstanceId)
			{
				_ownerActor.App.EventManager.QueueEvent(
					new TriggerEvent(_ownerActor.Id, eventType, otherActor.Id));
			}
		}

		private void SendCollisionEvent(ColliderEventType eventType, UnityCollision collision)
		{
			if (!_ownerActor.App.IsAuthoritativePeer && !_ownerActor.IsGrabbed)
			{
				return;
			}

			var otherActor = collision.collider.gameObject.GetComponent<Actor>();
			if (otherActor != null && otherActor.App.InstanceId == _ownerActor.App.InstanceId)
			{
				var sceneRoot = _ownerActor.App.SceneRoot.transform;

				var contacts = collision.contacts.Select((contact) =>
				{
					return new MREContactPoint()
					{
						Normal = sceneRoot.InverseTransformDirection(contact.normal).CreateMWVector3(),
						Point = sceneRoot.InverseTransformPoint(contact.point).CreateMWVector3(),
						Separation = contact.separation
					};
				});

				var collisionData = new CollisionData()
				{
					otherActorId = otherActor.Id,
					Contacts = contacts,
					Impulse = sceneRoot.InverseTransformDirection(collision.impulse).CreateMWVector3(),
					RelativeVelocity = collision.relativeVelocity.CreateMWVector3()
				};

				_ownerActor.App.EventManager.QueueEvent(
					new CollisionEvent(_ownerActor.Id, eventType, collisionData));
			}
		}
		*/
	}
}
