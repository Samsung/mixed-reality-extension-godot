// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Animation;
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Components;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Messaging;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.GodotHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

using GodotLight = Godot.Light3D;
using GodotRigidBody3D = Godot.RigidBody3D;
using GodotCollisionShape3D = Godot.CollisionShape3D;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using MixedRealityExtension.Util;
//using IVideoPlayer = MixedRealityExtension.PluginInterfaces.IVideoPlayer;
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.PluginInterfaces;

namespace MixedRealityExtension.Core
{
	/// <summary>
	/// Class that represents an actor in a mixed reality extension app.
	/// </summary>
	internal sealed partial class Actor : MixedRealityExtensionObject, ICommandHandlerContext, IActor
	{
		private static readonly Variant actorScript = new Actor().GetScript();

		public static Actor Instantiate(Node3D node3D)
		{
			ulong objId = node3D.GetInstanceId();
			var name = node3D.Name;

			node3D.SetScript(actorScript);
			var newNode = GD.InstanceFromId(objId) as Actor;
			newNode.Name = name;
			newNode.SetProcess(true);

			return newNode;
		}
		private Node3D TransformNode => (Node3D)_rigidbody ?? this;
		private GodotRigidBody3D _rigidbody;
		private GodotLight _light;
		private GodotCollisionShape3D _collider;
		private ColliderPatch _pendingColliderPatch;
		private LookAtComponent _lookAt;
		private ClippingBase _clipping;
		class MediaInstance
		{
			public Guid MediaAssetId { get; }

			//null until asset has finished loading
			public System.Object Instance { get; set; }

			public MediaInstance(Guid MediaAssetId)
			{
				this.MediaAssetId = MediaAssetId;
			}
		};

		private Dictionary<Guid, MediaInstance> _mediaInstances;
		private float _nextUpdateTime;
		private bool _grabbedLastSync = false;

		private MWScaledTransform _localTransform;
		private MWTransform _appTransform;

		private TransformLerper _transformLerper;

		private Dictionary<Type, ActorComponentBase> _components = new Dictionary<Type, ActorComponentBase>();

		private ActorComponentType _subscriptions = ActorComponentType.None;

		private ActorTransformPatch _rbTransformPatch;

		private MeshInstance3D meshInstance = null;

		internal MeshInstance3D MeshInstance3D
		{
			get
			{
				if (meshInstance == null)
				{
					meshInstance = Node3D.GetChild<MeshInstance3D>();
				}
				return meshInstance;
			}
			set
			{
				meshInstance = value;
			}
		}

		/// <summary>
		/// Checks if rigid body is simulated locally.
		/// </summary>
		internal bool IsSimulatedByLocalUser
		{
			get
			{
				return _isExclusiveToUser
					|| Owner.HasValue && App.LocalUser != null && Owner.Value == App.LocalUser.Id;
			}
		}

		private bool _isExclusiveToUser = false;

		#region IActor Properties - Public

		/// <inheritdoc />
		public IActor Parent => App.FindActor(ParentId);

		public new string Name
		{
			get => ((Node)this).Name;
			set => ((Node)this).Name = value;
		}

		private Guid? Owner = null;

		/// <inheritdoc />
		IMixedRealityExtensionApp IActor.App => base.App;

		/// <inheritdoc />
		public MWScaledTransform LocalTransform
		{
			get
			{
				if (_localTransform == null)
				{
					_localTransform = new MWScaledTransform();
					_localTransform.ToLocalTransform(TransformNode);
				}

				return _localTransform;
			}

			private set
			{
				_localTransform = value;
			}
		}

		/// <inheritdoc />
		public MWTransform AppTransform
		{
			get
			{
				if (_appTransform == null)
				{
					_appTransform = new MWTransform();
					_appTransform.ToAppTransform(TransformNode, App.SceneRoot);
				}

				return _appTransform;
			}

			private set
			{
				_appTransform = value;
			}
		}

		#endregion

		#region Properties - Internal

		internal Guid ParentId { get; set; } = Guid.Empty;

		internal RigidBody RigidBody3D { get; private set; }

		internal Light Light { get; private set; }

		internal IText Text { get; private set; }

		internal Collider Collider { get; private set; }

		internal Attachment Attachment { get; } = new Attachment();
		private Attachment _cachedAttachment = new Attachment();

		private Guid _materialId = Guid.Empty;
		private bool ListeningForMaterialChanges = false;

		internal Guid MaterialId
		{
			get
			{
				return _materialId;
			}
			set
			{
				_materialId = value;

				// look up and assign material, or default if none assigned
				if (_materialId != Guid.Empty)
				{
					var updatedMaterialId = _materialId;
					App.AssetManager.OnSet(_materialId, sharedMat =>
					{
						if (this == null || MeshInstance3D == null || _materialId != updatedMaterialId) return;

						MeshInstance3D.SetSurfaceOverrideMaterial(0, (Material)(sharedMat.Asset ?? MREAPI.AppsAPI.DefaultMaterial.Duplicate()));

						// keep this material up to date
						if (!ListeningForMaterialChanges)
						{
							App.AssetManager.AssetReferenceChanged += CheckMaterialReferenceChanged;
							ListeningForMaterialChanges = true;
						}
					});
				}
				else
				{
					MeshInstance3D.SetSurfaceOverrideMaterial(0, (Material)MREAPI.AppsAPI.DefaultMaterial.Duplicate());
					if (ListeningForMaterialChanges)
					{
						App.AssetManager.AssetReferenceChanged -= CheckMaterialReferenceChanged;
						ListeningForMaterialChanges = false;
					}
				}
			}
		}

		internal Guid MeshId { get; set; } = Guid.Empty;

		internal Mesh GodotMesh
		{
			get
			{
				return MeshInstance3D.Mesh;
			}
			set
			{
				MeshInstance3D.Mesh = value;
			}
		}

		internal bool Grabbable { get; private set; }

		internal bool IsGrabbed
		{
			get
			{
				var behaviorComponent = GetActorComponent<BehaviorComponent>();
				if (behaviorComponent != null && behaviorComponent.Behavior is ITargetBehavior targetBehavior)
				{
					return targetBehavior.IsGrabbed;
				}

				return false;
			}
		}

		internal UInt32 appearanceEnabled = UInt32.MaxValue;
		internal bool activeAndEnabled
		{
			get
			{
				bool parentEnabled = (Parent != null) ? (Parent as Actor).activeAndEnabled : true;
				uint userGroups = (App.LocalUser?.Groups ?? 1);
				return parentEnabled && (userGroups & appearanceEnabled) > 0;
			}
		}

		internal ITouchableBase Touchable { get; private set; }

		#endregion

		#region Methods - Internal

		internal ComponentT GetActorComponent<ComponentT>() where ComponentT : ActorComponentBase
		{
			if (_components.ContainsKey(typeof(ComponentT)))
			{
				return (ComponentT)_components[typeof(ComponentT)];
			}

			return null;
		}

		internal ComponentT GetOrCreateActorComponent<ComponentT>() where ComponentT : ActorComponentBase, new()
		{
			var component = GetActorComponent<ComponentT>();
			if (component == null)
			{
				component = new ComponentT();
				this.AddChild(component);
				component.AttachedActor = this;
				_components[typeof(ComponentT)] = component;
			}

			return component;
		}

		internal void SynchronizeApp(ActorComponentType? subscriptionsOverride = null)
		{
			if (CanSync())
			{
				var subscriptions = subscriptionsOverride.HasValue ? subscriptionsOverride.Value : _subscriptions;

				// Handle changes in game state and raise appropriate events for network updates.
				var actorPatch = new ActorPatch(Id);

				// We need to detect for changes in parent on the client, and handle updating the server.
				// But only update if the identified parent is not pending.
				var parentId = (Parent != null) ? Parent.Id : Guid.Empty;
				if (ParentId != parentId && App.FindActor(ParentId) != null)
				{
					// TODO @tombu - Determine if the new parent is an actor in OUR MRE.
					// TODO: Add in MRE ID's to help identify whether the new parent is in our MRE or not, not just
					// whether it is a MRE actor.
					ParentId = parentId;
					actorPatch.ParentId = ParentId;
				}

				if (!App.UsePhysicsBridge || RigidBody3D == null)
				{
					if (ShouldSync(subscriptions, ActorComponentType.Transform))
					{
						GenerateTransformPatch(actorPatch);
					}
				}

				if (ShouldSync(subscriptions, ActorComponentType.Rigidbody))
				{
					// we should include the velocities either when the old sync model is used
					// OR when there is an explicit subscription to it.
					GenerateRigidBody3DPatch(actorPatch,
						(!App.UsePhysicsBridge || subscriptions.HasFlag(ActorComponentType.RigidbodyVelocity)));
				}

				if (ShouldSync(ActorComponentType.Attachment, ActorComponentType.Attachment))
				{
					GenerateAttachmentPatch(actorPatch);
				}

				if (actorPatch.IsPatched())
				{
					App.EventManager.QueueEvent(new ActorChangedEvent(Id, actorPatch));
				}

				// If the actor is grabbed or was grabbed last time we synced and is not grabbed any longer,
				// then we always need to sync the transform.
				if (IsGrabbed || _grabbedLastSync)
				{
					var appTransform = new MWTransform();
					appTransform.ToAppTransform(TransformNode, App.SceneRoot);

					var actorCorrection = new ActorCorrection()
					{
						ActorId = Id,
						AppTransform = appTransform
					};

					App.EventManager.QueueEvent(new ActorCorrectionEvent(Id, actorCorrection));
				}

				// We update whether the actor was grabbed this sync to ensure we send one last transform update
				// on the sync when they are no longer grabbed.  This is the final transform update after the grab
				// is completed.  This should always be cached at the very end of the sync to ensure the value is valid
				// for any test calls to ShouldSync above.
				_grabbedLastSync = IsGrabbed;
			}
		}

		internal void ApplyPatch(ActorPatch actorPatch)
		{
			PatchExclusive(actorPatch.ExclusiveToUser);
			PatchName(actorPatch.Name);
			PatchOwner(actorPatch.Owner);
			PatchParent(actorPatch.ParentId);
			PatchAppearance(actorPatch.Appearance);
			PatchTransform(actorPatch.Transform);
			PatchLight(actorPatch.Light);
			PatchRigidBody3D(actorPatch.RigidBody3D);
			PatchCollider(actorPatch.Collider);
			PatchText(actorPatch.Text);
			PatchAttachment(actorPatch.Attachment);
			PatchLookAt(actorPatch.LookAt);
			PatchGrabbable(actorPatch.Grabbable);
			PatchSubscriptions(actorPatch.Subscriptions);
			PatchClipping(actorPatch.Clipping);
			PatchTouchable(actorPatch.Touchable);
		}

		internal void ApplyCorrection(ActorCorrection actorCorrection)
		{
			CorrectAppTransform(actorCorrection.AppTransform);
		}

		internal void SynchronizeEngine(ActorPatch actorPatch)
		{
			ApplyPatch(actorPatch);
		}

		internal void EngineCorrection(ActorCorrection actorCorrection)
		{
			ApplyCorrection(actorCorrection);
		}

		internal void ExecuteRigidBody3DCommands(RigidBodyCommands commandPayload, Action onCompleteCallback)
		{
			foreach (var command in commandPayload.CommandPayloads.OfType<ICommandPayload>())
			{
				App.ExecuteCommandPayload(this, command, null);
			}
			onCompleteCallback?.Invoke();
		}

		internal void Destroy()
		{
			CleanUp();
			this.QueueFree();
		}

		internal ActorPatch GeneratePatch(ActorPatch output = null, TargetPath path = null)
		{
			if (output == null)
			{
				output = new ActorPatch(Id);
			}

			var generateAll = path == null;
			if (!generateAll)
			{
				if (path.AnimatibleType != "actor") return output;
				output.Restore(path, 0);
			}
			else
			{
				output.RestoreAll();
			}

			if (generateAll || path.PathParts[0] == "transform")
			{
				if (generateAll || path.PathParts[1] == "local")
				{
					LocalTransform.ToLocalTransform(TransformNode);
					if (generateAll || path.PathParts[2] == "position")
					{
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "x")
						{
							output.Transform.Local.Position.X = LocalTransform.Position.X;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "y")
						{
							output.Transform.Local.Position.Y = LocalTransform.Position.Y;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "z")
						{
							output.Transform.Local.Position.Z = LocalTransform.Position.Z;
						}
					}
					if (generateAll || path.PathParts[2] == "rotation")
					{
						output.Transform.Local.Rotation.X = LocalTransform.Rotation.X;
						output.Transform.Local.Rotation.Y = LocalTransform.Rotation.Y;
						output.Transform.Local.Rotation.Z = LocalTransform.Rotation.Z;
						output.Transform.Local.Rotation.W = LocalTransform.Rotation.W;
					}
					if (generateAll || path.PathParts[2] == "scale")
					{
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "x")
						{
							output.Transform.Local.Scale.X = LocalTransform.Scale.X;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "y")
						{
							output.Transform.Local.Scale.Y = LocalTransform.Scale.Y;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "z")
						{
							output.Transform.Local.Scale.Z = LocalTransform.Scale.Z;
						}
					}
				}
				if (generateAll || path.PathParts[1] == "app")
				{
					AppTransform.ToAppTransform(TransformNode, App.SceneRoot);
					if (generateAll || path.PathParts[2] == "position")
					{
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "x")
						{
							output.Transform.App.Position.X = AppTransform.Position.X;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "y")
						{
							output.Transform.App.Position.Y = AppTransform.Position.Y;
						}
						if (generateAll || path.PathParts.Length == 3 || path.PathParts[3] == "z")
						{
							output.Transform.App.Position.Z = AppTransform.Position.Z;
						}
					}
					if (generateAll || path.PathParts[2] == "rotation")
					{
						output.Transform.App.Rotation.X = AppTransform.Rotation.X;
						output.Transform.App.Rotation.Y = AppTransform.Rotation.Y;
						output.Transform.App.Rotation.Z = AppTransform.Rotation.Z;
						output.Transform.App.Rotation.W = AppTransform.Rotation.W;
					}
				}
			}

			if (generateAll)
			{
				var rigidBody = PatchingUtilMethods.GeneratePatch(RigidBody3D, (GodotRigidBody3D)null,
					App.SceneRoot, !App.UsePhysicsBridge);

				ColliderPatch collider = null;
				var area = Node3D.GetChild<Area3D>();
				_collider = area?.GetChild<GodotCollisionShape3D>();
				if (_collider != null)
				{
					if (Collider == null)
					{
						Collider = Collider.Instantiate(area);
					}
					Collider.Initialize(_collider);
					collider = Collider.GenerateInitialPatch();
				}

				output.ParentId = ParentId;
				output.Name = Name;
				output.RigidBody3D = rigidBody;
				output.Collider = collider;
				output.Appearance = new AppearancePatch()
				{
					Enabled = appearanceEnabled,
					MaterialId = MaterialId,
					MeshId = MeshId
				};
			}

			return output;
		}
/*
		internal OperationResult EnableRigidBody3D(RigidBody3DPatch rigidBodyPatch)
		{
			if (AddRigidBody3D() != null)
			{
				if (rigidBodyPatch != null)
				{
					PatchRigidBody3D(rigidBodyPatch);
				}

				return new OperationResult()
				{
					ResultCode = OperationResultCode.Success
				};
			}

			return new OperationResult()
			{
				ResultCode = OperationResultCode.Error,
				Message = string.Format("Failed to create and enable the rigidbody for actor with id {0}", Id)
			};
		}

		internal OperationResult EnableLight(LightPatch lightPatch)
		{
			if (AddLight() != null)
			{
				if (lightPatch != null)
				{
					PatchLight(lightPatch);
				}

				return new OperationResult()
				{
					ResultCode = OperationResultCode.Success
				};
			}

			return new OperationResult()
			{
				ResultCode = OperationResultCode.Error,
				Message = string.Format("Failed to create and enable the light for actor with id {0}", Id)
			};
		}

		internal OperationResult EnableText(TextPatch textPatch)
		{
			if (AddText() != null)
			{
				if (textPatch != null)
				{
					PatchText(textPatch);
				}

				return new OperationResult()
				{
					ResultCode = OperationResultCode.Success
				};
			}

			return new OperationResult()
			{
				ResultCode = OperationResultCode.Error,
				Message = string.Format("Failed to create and enable the text object for actor with id {0}", Id)
			};
		}
*/
		// These two variables are for local use in the SendActorUpdate method to prevent unnecessary allocations.  Their
		// user should be limited to this function.
		private MWScaledTransform __methodVar_localTransform = new MWScaledTransform();
		private MWTransform __methodVar_appTransform = new MWTransform();
		internal void SendActorUpdate(ActorComponentType flags)
		{
			ActorPatch actorPatch = new ActorPatch(Id);

			if (flags.HasFlag(ActorComponentType.Transform))
			{
				__methodVar_localTransform.ToLocalTransform(TransformNode);
				__methodVar_appTransform.ToAppTransform(TransformNode, App.SceneRoot);

				actorPatch.Transform = new ActorTransformPatch()
				{
					Local = __methodVar_localTransform.AsPatch(),
					App = __methodVar_appTransform.AsPatch()
				};
			}

			//if ((flags & SubscriptionType.Rigidbody) != SubscriptionType.None)
			//{
			//    actorPatch.Transform = this.RigidBody3D.AsPatch();
			//}

			if (actorPatch.IsPatched())
			{
				App.EventManager.QueueEvent(new ActorChangedEvent(Id, actorPatch));
			}
		}

		#endregion

		#region MonoBehaviour Virtual Methods

		protected override void OnStart()
		{
			_rigidbody = this.GetChild<GodotRigidBody3D>();
			_light = this.GetChild<GodotLight>();
		}

		protected override void OnDestroyed()
		{
			// TODO @tombu, @eanders - We need to decide on the correct cleanup timing here for multiplayer, as this could cause a potential
			// memory leak if the engine deletes game objects, and we don't do proper cleanup here.
			//CleanUp();
			//App.OnActorDestroyed(this.Id);

			IHostAppUser hostAppUser = App.FindUser(Attachment.UserId)?.HostAppUser;
			if (hostAppUser != null)
			{
				hostAppUser.BeforeAvatarDestroyed -= UserInfo_BeforeAvatarDestroyed;
			}
/*FIXME
			if (_mediaInstances != null)
			{
				foreach (KeyValuePair<Guid, MediaInstance> mediaInstance in _mediaInstances)
				{
					DestroyMediaById(mediaInstance.Key, mediaInstance.Value);
				}
			}
*/
			if (App.UsePhysicsBridge)
			{
				if (RigidBody3D != null)
				{
					App.PhysicsBridge.removeRigidBody3D(Id);
				}
			}

			if (ListeningForMaterialChanges)
			{
				App.AssetManager.AssetReferenceChanged -= CheckMaterialReferenceChanged;
			}
		}

		protected override void InternalUpdate(float delta)
		{
			try
			{
				// TODO: Add ability to flag an actor for "high-frequency" updates
				if (Time.GetTicksMsec() >= _nextUpdateTime)
				{

					_nextUpdateTime = Time.GetTicksMsec();// + 200f + (float)GD.RandRange(-100, 100);
					SynchronizeApp();

					// Give components the opportunity to synchronize the app.
					foreach (var component in _components.Values)
					{
						component.SynchronizeComponent();
					}
				}
			}
			catch (Exception e)
			{
				App?.Logger.LogError($"Failed to synchronize app.  Exception: {e.Message}\nStackTrace: {e.StackTrace}");
			}

			_transformLerper?.Update();
		}

		protected override void InternalFixedUpdate()
		{
			try
			{
				if (_rigidbody == null)
				{
					return;
				}

				RigidBody3D = RigidBody3D ?? new RigidBody(_rigidbody, App.SceneRoot);
				RigidBody3D.Update();
				// TODO: Send this update if actor is set to "high-frequency" updates
				//Actor.SynchronizeApp();
			}
			catch (Exception e)
			{
				App.Logger.LogError($"Failed to update rigid body.  Exception: {e.Message}\nStackTrace: {e.StackTrace}");
			}
		}

		#endregion

		#region Methods - Private

		private Attachment FindAttachmentInHierarchy()
		{
			Attachment FindAttachmentRecursive(Actor actor)
			{
				if (actor == null)
				{
					return null;
				}
				if (actor.Attachment.AttachPoint != null && actor.Attachment.UserId != Guid.Empty)
				{
					return actor.Attachment;
				}
				return FindAttachmentRecursive(actor.Parent as Actor);
			};
			return FindAttachmentRecursive(this);
		}

		private void DetachFromAttachPointParent()
		{
			try
			{
				{
					var attachmentComponent = GetParent().GetChildren<MREAttachmentComponent>()
						.FirstOrDefault(component =>
							component.Actor != null &&
							component.Actor.Id == Id &&
							component.Actor.AppInstanceId == AppInstanceId &&
							component.UserId == _cachedAttachment.UserId);

					if (attachmentComponent != null)
					{
						attachmentComponent.Actor = null;
						attachmentComponent.QueueFree();
					}
				}
			}
			catch (Exception e)
			{
				App.Logger.LogError($"Exception: {e.Message}\nStackTrace: {e.StackTrace}");
			}
		}

		private bool PerformAttach()
		{
			// Assumption: Attachment state has changed and we need to (potentially) detach and (potentially) reattach.
			try
			{
				DetachFromAttachPointParent();

				IHostAppUser hostAppUser = App.FindUser(Attachment.UserId)?.HostAppUser;
				if (hostAppUser != null &&
					(Attachment.UserId != App.LocalUser?.Id || App.GrantedPermissions.HasFlag(Permissions.UserInteraction)))
				{
					hostAppUser.BeforeAvatarDestroyed -= UserInfo_BeforeAvatarDestroyed;

					Node attachPoint = hostAppUser.GetAttachPoint(Attachment.AttachPoint);
					if (attachPoint != null)
					{
						var attachmentComponent = attachPoint.AddNode(new MREAttachmentComponent());
						attachmentComponent.Actor = this;
						attachmentComponent.UserId = Attachment.UserId;
						attachmentComponent.Transform = this.Transform;
						hostAppUser.BeforeAvatarDestroyed += UserInfo_BeforeAvatarDestroyed;
						// FIXME: I have no idea how to bind user parameters.
						//Connect("tree_exited", new Callable(this, nameof(ActorTreeExited)), new Godot.Collections.Array() { attachmentComponent });
						return true;
					}
				}
			}
			catch (Exception e)
			{
				App.Logger.LogError($"Exception: {e.Message}\nStackTrace: {e.StackTrace}");
			}

			return false;
		}

		private void ActorTreeExited(Node3D attachmentComponent)
		{
			attachmentComponent.QueueFree();
		}

		private void UserInfo_BeforeAvatarDestroyed()
		{
			// Remember the original local transform.
			MWScaledTransform cachedTransform = LocalTransform;

			// Detach from parent. This will preserve the world transform (changing the local transform).
			// This is desired so that the actor doesn't change position, but we must restore the local
			// transform when reattaching.
			DetachFromAttachPointParent();

			IHostAppUser hostAppUser = App.FindUser(Attachment.UserId)?.HostAppUser;
			if (hostAppUser != null)
			{
				void Reattach()
				{
					// Restore the local transform and reattach.
					hostAppUser.AfterAvatarCreated -= Reattach;
					// In the interim time this actor might have been destroyed.
					/*FIXME
					if (transform != null)
					{
						transform.localPosition = cachedTransform.Position.ToVector3();
						transform.localRotation = cachedTransform.Rotation.ToQuaternion();
						transform.localScale = cachedTransform.Scale.ToVector3();
						PerformAttach();
					}
					*/
				}

				// Register for a callback once the avatar is recreated.
				hostAppUser.AfterAvatarCreated += Reattach;
			}
		}

		private IText AddText()
		{
			Text = MREAPI.AppsAPI.TextFactory.CreateText(this);
			return Text;
		}

		private Light AddLight(LightType? lightType)
		{
			if (_light == null)
			{
				switch (lightType)
				{
					case LightType.Spot:
						_light = new SpotLight3D()
						{
							SpotAngle = 15,
							SpotRange = 10,
						};
						break;
					case LightType.Point:
						_light = new OmniLight3D()
						{
							OmniRange = 10,
						};
						break;
					case LightType.Directional:
						_light = new DirectionalLight3D();
						break;
				}
				_light.ShadowEnabled = true;
				_light.ShadowBias = 0.01f;
				AddChild(_light);
				Light = new Light(_light);
			}
			return Light;
		}

		void OnRigidBody3DGrabbed(object sender, ActionStateChangedArgs args)
		{
			if (App.UsePhysicsBridge)
			{
				if (args.NewState != ActionState.Performing)
				{
					if (_isExclusiveToUser)
					{
						// if rigid body is exclusive to user, manage rigid body directly
						if (args.NewState == ActionState.Started || RigidBody3D.IsKinematic)
						{
							_rigidbody.FreezeMode = GodotRigidBody3D.FreezeModeEnum.Kinematic;
						}
					}
					else
					{
						// if rigid body needs to be synchronized, handle it through physics bridge
						if (args.NewState == ActionState.Started)
						{
							// set to kinematic when grab starts
							App.PhysicsBridge.setKeyframed(Id, true);
						}
						else
						{
							// on end of grab, return to original value
							App.PhysicsBridge.setKeyframed(Id, RigidBody3D.IsKinematic);
						}
					}
				}
			}
		}

		private RigidBody AddRigidBody3D()
		{
			if (_rigidbody == null)
			{
				var parent = GetParent();
				_rigidbody = new GodotRigidBody3D()
				{
					PhysicsMaterialOverride = new PhysicsMaterial(),
					GlobalTransform = GlobalTransform
				};

				parent.AddChild(_rigidbody);
				parent.RemoveChild(this);
				_rigidbody.AddChild(this);
				this.Transform = Transform3D.Identity;

				RigidBody3D = new RigidBody(_rigidbody, App.SceneRoot);

				if (App.UsePhysicsBridge)
				{
					// Add rigid body to physics bridge only when source is known.
					// Otherwise, do it once source is provided.
					if (Owner.HasValue && !_isExclusiveToUser)
					{
						App.PhysicsBridge.addRigidBody(Id, _rigidbody, Owner.Value, RigidBody3D.IsKinematic);
					}
				}

				var behaviorComponent = GetActorComponent<BehaviorComponent>();
				if (behaviorComponent != null && behaviorComponent.Context is TargetBehaviorContext targetContext)
				{
					var targetBehavior = (ITargetBehavior)targetContext.Behavior;
					if (targetBehavior.Grabbable)
					{
						targetContext.GrabAction.ActionStateChanged += OnRigidBody3DGrabbed;
					}
				}
			}
			return RigidBody3D;
		}

		/// <summary>
		/// Precondition: The mesh referred to by MeshId is loaded and available for use.
		/// </summary>
		/// <param name="colliderPatch"></param>
		private void SetCollider(ColliderPatch colliderPatch)
		{
			if (colliderPatch == null || colliderPatch.Geometry == null)
			{
				return;
			}

			var colliderGeometry = colliderPatch.Geometry;
			var colliderType = colliderGeometry.Shape;

			if (colliderType == ColliderType.Auto)
			{
				colliderGeometry = App.AssetManager.GetById(MeshId).Value.ColliderGeometry;
				colliderType = colliderGeometry.Shape;
			}

			if (_collider != null)
			{
				if (Collider.Shape == colliderType)
				{
					// We have a collider already of the same type as the desired new geometry.
					// Update its values instead of removing and adding a new one.
					colliderGeometry.Patch(App, _collider);
					return;
				}
				else
				{
					_collider.Free();
					_collider = null;
				}
			}

			GodotCollisionShape3D godotCollisionShape3D = null;

			switch (colliderType)
			{
				case ColliderType.Box:
					var boxCollider = new CollisionShape3D() { Shape = new BoxShape3D() };
					colliderGeometry.Patch(App, boxCollider);
					godotCollisionShape3D = boxCollider;
					break;
				case ColliderType.Sphere:
					var sphereCollider =  new CollisionShape3D() { Shape = new SphereShape3D() };
					colliderGeometry.Patch(App, sphereCollider);
					godotCollisionShape3D = sphereCollider;
					break;
				case ColliderType.Capsule:
					var capsuleCollider =  new CollisionShape3D() { Shape = new CapsuleShape3D() };
					colliderGeometry.Patch(App, capsuleCollider);
					godotCollisionShape3D = capsuleCollider;
					break;
				case ColliderType.Cylinder:
					var cylinderCollider =  new CollisionShape3D() { Shape = new CylinderShape3D() };
					colliderGeometry.Patch(App, cylinderCollider);
					godotCollisionShape3D = cylinderCollider;
					break;
				case ColliderType.Mesh:
					var meshCollider = new CollisionShape3D() { Shape = new ConcavePolygonShape3D() };
					colliderGeometry.Patch(App, meshCollider);
					godotCollisionShape3D = meshCollider;
					break;
				default:
					App.Logger.LogWarning("Cannot add the given collider type to the actor " +
						$"during runtime.  Collider Type: {colliderPatch.Geometry.Shape}");
					break;
			}

			_collider = godotCollisionShape3D;

			// update bounciness and frictions
			if (_rigidbody != null)
			{
				if (colliderPatch.Bounciness.HasValue)
					_rigidbody.PhysicsMaterialOverride.Bounce = colliderPatch.Bounciness.Value;
				if (colliderPatch.StaticFriction.HasValue)
				{
					GD.PushWarning("StaticFriction is not supported in godot mre. please use DynamicFriction instead.");
				}
				if (colliderPatch.DynamicFriction.HasValue)
					_rigidbody.PhysicsMaterialOverride.Friction = colliderPatch.DynamicFriction.Value;
			}
			if (Collider == null)
			{
				Collider = new Collider();
				Node3D.AddChild(Collider);
			}
			Collider.AddChild(_collider);
			Collider.Initialize(_collider, colliderPatch.Geometry.Shape);
			return;
		}

		private void PatchParent(Guid? parentId)
		{
			if (!parentId.HasValue)
			{
				return;
			}

			var newParent = App.FindActor(parentId.Value);
			if (parentId.Value != ParentId && parentId.Value == Guid.Empty)
			{
				// clear parent
				ParentId = Guid.Empty;
				TransformNode.GetParent().RemoveChild(TransformNode);
				App.SceneRoot.AddChild(TransformNode);
			}
			else if (parentId.Value != ParentId && newParent != null)
			{
				// reassign parent
				ParentId = parentId.Value;
				TransformNode.GetParent().RemoveChild(TransformNode);
				((Actor)newParent).Node3D.AddChild(TransformNode);
			}
			else if (parentId.Value != ParentId)
			{
				// queue parent reassignment
				ParentId = newParent == null ? Guid.Empty : parentId.Value;
				App.ProcessActorCommand(parentId.Value, new LocalCommand()
				{
					Command = () =>
					{
						var freshParent = App.FindActor(parentId.Value) as Actor;
						if (this != null && freshParent != null && Parent != freshParent)
						{
							TransformNode.GetParent().RemoveChild(TransformNode);
							freshParent.Node3D.AddChild(TransformNode);
						}
					}
				}, null);
			}
		}

		private void PatchName(string nameOrNull)
		{
			if (nameOrNull != null)
			{
				Name = nameOrNull;
				((Node)this).Name = Name;
			}
		}

		private void PatchExclusive(Guid? exclusiveToUser)
		{
			if (App.UsePhysicsBridge && exclusiveToUser.HasValue)
			{
				// Should be set only once when actor is initialized
				// and only for single user who receives the patch.
				// The comparison check is not actually required.
				_isExclusiveToUser = App.LocalUser.Id == exclusiveToUser.Value;
			}
		}

		private void PatchOwner(Guid? ownerOrNull)
		{
			if (App.UsePhysicsBridge)
			{
				if (ownerOrNull.HasValue)
				{
					if (RigidBody3D != null)
					{
						if (!_isExclusiveToUser)
						{
							if (Owner.HasValue) // test the old value
							{
								// if body is already registered to physics bridge, just set the new owner
								App.PhysicsBridge.setRigidBody3DOwnership(Id, ownerOrNull.Value, RigidBody3D.IsKinematic);
							}
							else
							{
								// if this is first time owner is set, add body to physics bridge
								App.PhysicsBridge.addRigidBody(Id, _rigidbody, ownerOrNull.Value, RigidBody3D.IsKinematic);
							}

							Owner = ownerOrNull;

							// If object is grabbed make it kinematic
							if (IsSimulatedByLocalUser && IsGrabbed)
							{
								App.PhysicsBridge.setKeyframed(Id, true);
							}
						}
					}
					else
					{
						Owner = ownerOrNull;
					}

				}
			}
		}

		private void PatchAppearance(AppearancePatch appearance)
		{
			if (appearance == null)
			{
				return;
			}

			bool forceUpdateRenderer = false;

			// update renderers
			if (appearance.MaterialId != null || appearance.MeshId != null)
			{
				// patch mesh
				if (appearance.MeshId != null)
				{
					MeshId = appearance.MeshId.Value;
				}

				// apply mesh/material to game object
				if (MeshId != Guid.Empty)
				{
					// guarantee meshInstance
					if (meshInstance == null || !Godot.Object.IsInstanceValid(meshInstance))
					{
						meshInstance = new MeshInstance3D();
						Node3D.AddChild(meshInstance);

						forceUpdateRenderer = true;
					}

					// look up and assign mesh
					var updatedMeshId = MeshId;
					App.AssetManager.OnSet(MeshId, sharedMesh =>
					{
						if (MeshId != updatedMeshId) return;
						GodotMesh = (Mesh)sharedMesh.Asset;
						meshInstance.SetSurfaceOverrideMaterial(0, (Material)MREAPI.AppsAPI.DefaultMaterial.Duplicate());
						if (Collider != null && Collider.Shape == ColliderType.Auto)
						{
							SetCollider(new ColliderPatch()
							{
								Geometry = new AutoColliderGeometry()
							});
						}
					});

					// patch material
					if (appearance.MaterialId != null)
					{
						MaterialId = appearance.MaterialId.Value;
					}
				}
				// clean up unused components
				else
				{
					if (MeshInstance3D != null)
						MeshInstance3D.QueueFree();
					if (Collider != null && Collider.Shape == ColliderType.Auto)
					{
						_collider.Free();
						Collider.QueueFree();
						_collider = null;
						Collider = null;
					}
				}
			}

			// apply visibility after renderer updated
			if (appearance.Enabled != null || forceUpdateRenderer)
			{
				if (appearance.Enabled != null)
				{
					appearanceEnabled = appearance.Enabled.Value;
				}
				ApplyVisibilityUpdate(this);
			}
		}

		internal static void ApplyVisibilityUpdate(Actor actor, bool force = false)
		{
			// Note: MonoBehaviours don't support conditional access (actor.Renderer?.enabled)
			if (actor != null)
			{
				actor.Node3D.Visible = actor.activeAndEnabled;

				foreach (var child in actor.App.FindChildren(actor.Id))
				{
					ApplyVisibilityUpdate(child, force);
				}
			}
		}

		/// <summary>
		/// Precondition: Asset identified by `id` exists, and is a material.
		/// </summary>
		/// <param name="id"></param>
		private void CheckMaterialReferenceChanged(Guid id)
		{
			if (this != null && MaterialId == id && MeshInstance3D != null)
			{
				MeshInstance3D.SetSurfaceOverrideMaterial(0, (Material)App.AssetManager.GetById(id).Value.Asset);
			}
		}

		private void PatchTransform(ActorTransformPatch transformPatch)
		{
			if (transformPatch != null)
			{
				if (RigidBody3D == null)
				{
					// Apply local first.
					if (transformPatch.Local != null)
					{
						Node3D.ApplyLocalPatch(LocalTransform, transformPatch.Local);
					}

					// Apply app patch second to ensure it overrides any duplicate values from the local patch.
					// App transform patching always wins over local, except for scale.
					if (transformPatch.App != null)
					{
						Node3D.ApplyAppPatch(App.SceneRoot, AppTransform, transformPatch.App);
					}
				}
				else
				{
					// We need to update transform only for the simulation owner,
					// others will get update through PhysicsBridge.
					if (!App.UsePhysicsBridge || IsSimulatedByLocalUser)
					{
						PatchTransformWithRigidBody3D(transformPatch);
					}
				}
			}
		}

		private void PatchTransformWithRigidBody3D(ActorTransformPatch transformPatch)
		{
			if (_rigidbody == null)
			{
				return;
			}

			RigidBody.RigidBodyTransformUpdate transformUpdate = new RigidBody.RigidBodyTransformUpdate();
			if (transformPatch.Local != null)
			{
				var parent = TransformNode.GetParent() as Node3D;
				// In case of rigid body:
				// - Apply scale directly.
				_rigidbody.Scale = _rigidbody.Scale.GetPatchApplied(LocalTransform.Scale.ApplyPatch(transformPatch.Local.Scale));

				// - Apply position and rotation via rigid body from local to world space.
				if (transformPatch.Local.Position != null)
				{
					var localPosition = LocalTransform.Position.ApplyPatch(transformPatch.Local.Position).ToVector3();
					localPosition.z *= -1;
					transformUpdate.Position = parent.ToGlobal(localPosition);
				}

				if (transformPatch.Local.Rotation != null)
				{
					var localRotation = LocalTransform.Rotation.ApplyPatch(transformPatch.Local.Rotation).ToQuaternion();
					localRotation.x *= -1;
					localRotation.y *= -1;
					transformUpdate.Rotation = parent.GlobalTransform.basis.GetRotationQuaternion() * localRotation;
				}
			}

			if (transformPatch.App != null)
			{
				var appTransform = App.SceneRoot;

				if (transformPatch.App.Position != null)
				{
					// New app space position.
					var newAppPos = appTransform.ToLocal(Transform.origin)
						.GetPatchApplied(AppTransform.Position.ApplyPatch(transformPatch.App.Position));

					// Transform new position to world space.
					transformUpdate.Position = appTransform.ToGlobal(newAppPos);
				}

				if (transformPatch.App.Rotation != null)
				{
					// New app space rotation
					var newAppRot = new Quaternion(TransformNode.GlobalTransform.basis.GetRotationQuaternion() * appTransform.Rotation)
						.GetPatchApplied(AppTransform.Rotation.ApplyPatch(transformPatch.App.Rotation));

					// Transform new app rotation to world space.
					transformUpdate.Rotation = newAppRot * TransformNode.GlobalTransform.basis.GetRotationQuaternion();
				}
			}

			// Queue update to happen in the fixed update
			RigidBody3D.SynchronizeEngine(transformUpdate);
		}

		private void CorrectAppTransform(MWTransform transform)
		{
			if (transform == null)
			{
				return;
			}

			if (RigidBody3D == null)
			{
				// We need to lerp at the transform level with the transform lerper.
				if (_transformLerper == null)
				{
					_transformLerper = new TransformLerper(TransformNode);
				}

				// Convert the app relative transform for the correction to world position relative to our app root.
				Vector3? newPos = null;
				Quaternion? newRot = null;

				if (transform.Position != null)
				{
					Vector3 appPos;
					appPos.x = transform.Position.X;
					appPos.y = transform.Position.Y;
					appPos.z = -transform.Position.Z;
					newPos = App.SceneRoot.ToGlobal(appPos);
				}

				if (transform.Rotation != null)
				{
					Quaternion appRot;
					appRot.w = transform.Rotation.W;
					appRot.x = -transform.Rotation.X;
					appRot.y = -transform.Rotation.Y;
					appRot.z = transform.Rotation.Z;
					newRot = App.SceneRoot.GlobalTransform.basis.GetRotationQuaternion() * appRot;
				}

				// We do not pass in a value for the update period at this point.  We will be adding in lag
				// prediction for the network here in the future once that is more fully fleshed out.
				_transformLerper.SetTarget(newPos, newRot);
			}
			else
			{
				// nothing to do this should be handled by the physics channel

				if (!App.UsePhysicsBridge)
				{
					// Lerping and correction needs to happen at the rigid body level here to
					// not interfere with physics simulation.  This will change with kinematic being
					// enabled on a rigid body for when it is grabbed.  We do not support this currently,
					// and thus do not interpolate the actor.  Just set the position for the rigid body.

					_rbTransformPatch = _rbTransformPatch ?? new ActorTransformPatch()
					{
						App = new TransformPatch()
						{
							Position = new Vector3Patch(),
							Rotation = new QuaternionPatch()
						}
					};

					if (transform.Position != null)
					{
						_rbTransformPatch.App.Position.X = transform.Position.X;
						_rbTransformPatch.App.Position.Y = transform.Position.Y;
						_rbTransformPatch.App.Position.Z = transform.Position.Z;
					}
					else
					{
						_rbTransformPatch.App.Position = null;
					}

					if (transform.Rotation != null)
					{
						_rbTransformPatch.App.Rotation.W = transform.Rotation.W;
						_rbTransformPatch.App.Rotation.X = transform.Rotation.X;
						_rbTransformPatch.App.Rotation.Y = transform.Rotation.Y;
						_rbTransformPatch.App.Rotation.Z = transform.Rotation.Z;
					}
					else
					{
						_rbTransformPatch.App.Rotation = null;
					}

					PatchTransformWithRigidBody3D(_rbTransformPatch);
				}
			}
		}

		private void PatchLight(LightPatch lightPatch)
		{
			if (lightPatch != null)
			{
				if (Light == null)
				{
					AddLight(lightPatch.Type);
				}
				Light.SynchronizeEngine(lightPatch);
			}
		}

		private void PatchRigidBody3D(RigidBodyPatch rigidBodyPatch)
		{
			if (rigidBodyPatch != null)
			{
				bool patchVelocities = !App.UsePhysicsBridge || IsSimulatedByLocalUser;

				bool wasKinematic;

				if (RigidBody3D == null)
				{
					AddRigidBody3D();

					wasKinematic = RigidBody3D.IsKinematic;

					RigidBody3D.ApplyPatch(rigidBodyPatch, patchVelocities);
				}
				else
				{
					wasKinematic = RigidBody3D.IsKinematic;

					// Queue update to happen in the fixed update
					RigidBody3D.SynchronizeEngine(rigidBodyPatch, patchVelocities);
				}

				if (App.UsePhysicsBridge)
				{
					if (rigidBodyPatch.IsKinematic.HasValue && rigidBodyPatch.IsKinematic.Value != wasKinematic)
					{
						App.PhysicsBridge.setKeyframed(Id, rigidBodyPatch.IsKinematic.Value);
					}
				}
			}
		}

		private void PatchText(TextPatch textPatch)
		{
			if (textPatch != null)
			{
				if (Text == null)
				{
					AddText();
				}
				Text.SynchronizeEngine(textPatch);
			}
		}

		private int colliderGeneration = 0;
		private void PatchCollider(ColliderPatch colliderPatch)
		{
			if (colliderPatch != null)
			{
				// A collider patch that contains collider geometry signals that we need to update the
				// collider to match the desired geometry.
				if (colliderPatch.Geometry != null)
				{
					var runningGeneration = ++colliderGeneration;
					// must wait for mesh load before auto type will work
					if (colliderPatch.Geometry.Shape == ColliderType.Auto && App.AssetManager.GetById(MeshId) == null)
					{
						var runningMeshId = MeshId;
						_pendingColliderPatch = colliderPatch;
						App.AssetManager.OnSet(MeshId, _ =>
						{
							if (runningMeshId != MeshId || runningGeneration != colliderGeneration) return;
							SetCollider(_pendingColliderPatch);
							Collider?.SynchronizeEngine(_pendingColliderPatch);
							_pendingColliderPatch = null;
						});
					}
					// every other kind of geo patch
					else
					{
						_pendingColliderPatch = null;
						SetCollider(colliderPatch);
					}
				}

				// If we're waiting for the auto mesh, don't apply any patches until it completes.
				// Instead, accumulate changes in the pending collider patch
				if (_pendingColliderPatch != null && _pendingColliderPatch != colliderPatch)
				{
					if (colliderPatch.Enabled.HasValue)
						_pendingColliderPatch.Enabled = colliderPatch.Enabled.Value;
					if (colliderPatch.IsTrigger.HasValue)
						_pendingColliderPatch.IsTrigger = colliderPatch.IsTrigger.Value;

					if (_rigidbody != null)
					{
						if (colliderPatch.Bounciness.HasValue)
							_rigidbody.PhysicsMaterialOverride.Bounce = colliderPatch.Bounciness.Value;
						if (colliderPatch.StaticFriction.HasValue)
						{
							GD.PushWarning("StaticFriction is not supported in godot mre. please use DynamicFriction instead.");
						}
						if (colliderPatch.DynamicFriction.HasValue)
							_rigidbody.PhysicsMaterialOverride.Friction = colliderPatch.DynamicFriction.Value;
					}
				}
				else if (_pendingColliderPatch == null)
				{
					Collider?.SynchronizeEngine(colliderPatch);
				}
			}
		}

		private void PatchAttachment(AttachmentPatch attachmentPatch)
		{
			if (attachmentPatch != null && attachmentPatch.IsPatched() && !attachmentPatch.Equals(Attachment))
			{
				Attachment.ApplyPatch(attachmentPatch);
				if (!PerformAttach())
				{
					Attachment.Clear();
				}
			}
		}

		private void PatchLookAt(LookAtPatch lookAtPatch)
		{
			if (lookAtPatch != null)
			{
				if (_lookAt == null)
				{
					_lookAt = GetOrCreateActorComponent<LookAtComponent>();
				}
				_lookAt.ApplyPatch(lookAtPatch);
			}
		}

		private void PatchGrabbable(bool? grabbable)
		{
			if (grabbable != null && grabbable.Value != Grabbable)
			{
				// Update existing behavior or add a basic target behavior if there isn't one already.
				var behaviorComponent = GetActorComponent<BehaviorComponent>();
				if (behaviorComponent == null)
				{
					// NOTE: We need to have the default behavior on an actor be a button for now in the case we want the actor
					// to be able to be grabbed on all controller types for host apps.  This will be a base Target behavior once we
					// update host apps to handle button conflicts.
					behaviorComponent = GetOrCreateActorComponent<BehaviorComponent>();
					var context = BehaviorContextFactory.CreateContext(BehaviorType.Button, this, new WeakReference<MixedRealityExtensionApp>(App));

					if (context == null)
					{
						GD.PushError("Failed to create a behavior context.  Grab will not work without one.");
						return;
					}

					behaviorComponent.SetBehaviorContext(context);
				}

				if (behaviorComponent.Context is TargetBehaviorContext targetContext)
				{
					if (RigidBody3D != null)
					{
						// for rigid body we need callbacks for the physics bridge
						var targetBehavior = (ITargetBehavior)targetContext.Behavior;
						bool wasGrabbable = targetBehavior.Grabbable;
						targetBehavior.Grabbable = grabbable.Value;

						if (wasGrabbable != grabbable.Value)
						{
							if (grabbable.Value)
							{
								targetContext.GrabAction.ActionStateChanged += OnRigidBody3DGrabbed;
							}
							else
							{
								targetContext.GrabAction.ActionStateChanged -= OnRigidBody3DGrabbed;
							}
						}
					}
					else
					{
						// non-rigid body context
						((ITargetBehavior)behaviorComponent.Behavior).Grabbable = grabbable.Value;
					}
				}
				Grabbable = grabbable.Value;
			}
		}

		private void PatchTouchable(TouchablePatch touchablePatch)
		{
			if (touchablePatch != null)
			{
				if (touchablePatch.Type != TouchableType.None && Touchable == null)
				{
					// Update existing behavior or add a basic target behavior if there isn't one already.
					var behaviorComponent = GetActorComponent<BehaviorComponent>();
					if (behaviorComponent == null)
					{
						// NOTE: We need to have the default behavior on an actor be a button for now in the case we want the actor
						// to be able to be touched on all controller types for host apps.  This will be a base Target behavior once we
						// update host apps to handle button conflicts.
						behaviorComponent = GetOrCreateActorComponent<BehaviorComponent>();
						var context = BehaviorContextFactory.CreateContext(BehaviorType.Button, this, new WeakReference<MixedRealityExtensionApp>(App));

						if (context == null)
						{
							GD.PushError("Failed to create a behavior context.  Touch will not work without one.");
							return;
						}

						behaviorComponent.SetBehaviorContext(context);
					}

					if (behaviorComponent.Context is TargetBehaviorContext targetContext)
					{
						ITouchableBase touchableBase = null;
						switch (touchablePatch.Type)
						{
							case TouchableType.Surface:
								var touchablePlane = new TouchablePlane(this) { Name = "TouchablePlane" };
								touchablePlane.ApplyPatch(touchablePatch);
								touchableBase = touchablePlane;
								break;
							case TouchableType.Volume:
								//TODO: touchable volume
								break;
						}
						((ITargetBehavior)behaviorComponent.Behavior).Touchable = touchableBase;
						Touchable = touchableBase;
					}
				}
				else if (touchablePatch.Type == TouchableType.None && Touchable != null)
				{
					var behaviorComponent = GetActorComponent<BehaviorComponent>();
					var oldTouchable = ((ITargetBehavior)behaviorComponent.Behavior).Touchable;
					RemoveChild((Node3D)oldTouchable);
					((ITargetBehavior)behaviorComponent.Behavior).Touchable = null;
					Touchable = null;
				}
			}
		}

		private void PatchSubscriptions(IEnumerable<ActorComponentType> subscriptions)
		{
			if (subscriptions != null)
			{
				_subscriptions = ActorComponentType.None;
				foreach (var subscription in subscriptions)
				{
					_subscriptions |= subscription;
				}
			}
		}

		private void PatchClipping(ClippingPatch clippingPatch)
		{
			if (clippingPatch != null)
			{
				if (_clipping == null)
				{
					_clipping = new ClippingBox();
					AddChild(_clipping);
				}
				_clipping.ApplyPatch(clippingPatch);
			}
		}

		private void GenerateTransformPatch(ActorPatch actorPatch)
		{
			var transformPatch = new ActorTransformPatch()
			{
				Local = PatchingUtilMethods.GenerateLocalTransformPatch(LocalTransform, TransformNode),
				App = PatchingUtilMethods.GenerateAppTransformPatch(AppTransform, TransformNode, App.SceneRoot)
			};

			LocalTransform.ToLocalTransform(TransformNode);
			AppTransform.ToAppTransform(TransformNode, App.SceneRoot);

			actorPatch.Transform = transformPatch.IsPatched() ? transformPatch : null;
		}

		private void GenerateRigidBody3DPatch(ActorPatch actorPatch, bool addVelocities)
		{
			if (_rigidbody != null && RigidBody3D != null)
			{
				// convert to a RigidBody3D and build a patch from the old one to this one.
				var rigidBodyPatch = PatchingUtilMethods.GeneratePatch(RigidBody3D, _rigidbody,
					App.SceneRoot, addVelocities);

				if (rigidBodyPatch != null && rigidBodyPatch.IsPatched())
				{
					actorPatch.RigidBody3D = rigidBodyPatch;
				}

				RigidBody3D.Update(_rigidbody);
			}
		}

		private void GenerateAttachmentPatch(ActorPatch actorPatch)
		{
			actorPatch.Attachment = Attachment.GeneratePatch(_cachedAttachment);
			if (actorPatch.Attachment != null)
			{
				_cachedAttachment.CopyFrom(Attachment);
			}
		}

		private void CleanUp()
		{
			var behaviorComponent = GetActorComponent<BehaviorComponent>();
			if (behaviorComponent != null && behaviorComponent.Context is TargetBehaviorContext targetContext)
			{
				var targetBehavior = (ITargetBehavior)targetContext.Behavior;
				if (RigidBody3D != null && Grabbable)
				{
					targetContext.GrabAction.ActionStateChanged -= OnRigidBody3DGrabbed;
				}
			}

			if (App.UsePhysicsBridge)
			{
				if (RigidBody3D != null)
				{
					App.PhysicsBridge.removeRigidBody3D(Id);
				}
			}

			foreach (var component in _components.Values)
			{
				component.CleanUp();
			}

			if (_rigidbody != null)
			{
				_rigidbody.QueueFree();
			}
		}

		private bool ShouldSync(ActorComponentType subscriptions, ActorComponentType flag)
		{
			// We do not want to send actor updates until we're fully joined to the app.
			// TODO: We shouldn't need to do this check. The engine shouldn't try to send
			// updates until we're fully joined to the app.
			if (!(App.Protocol is Messaging.Protocols.Execution))
			{
				return false;
			}

			// If the actor has a rigid body then always sync the transform and the rigid body.
			// but not the velocities (due to bandwidth), sync only when there is an explicit subscription for the velocities
			if (RigidBody3D != null)
			{
				subscriptions |= ActorComponentType.Transform;
				subscriptions |= ActorComponentType.Rigidbody;
			}

			Attachment attachmentInHierarchy = FindAttachmentInHierarchy();
			bool inAttachmentHeirarchy = (attachmentInHierarchy != null);
			bool inOwnedAttachmentHierarchy = (inAttachmentHeirarchy && LocalUser != null && attachmentInHierarchy.UserId == LocalUser.Id);

			// Don't sync anything if the actor is in an attachment hierarchy on a remote avatar.
			if (inAttachmentHeirarchy && !inOwnedAttachmentHierarchy)
			{
				subscriptions = ActorComponentType.None;
			}

			if (subscriptions.HasFlag(flag))
			{
				return
					((App.OperatingModel == OperatingModel.ServerAuthoritative) ||
					(RigidBody3D == null &&
						((App.IsAuthoritativePeer ||
						inOwnedAttachmentHierarchy) && !IsGrabbed)) ||
					(RigidBody3D != null &&
						IsSimulatedByLocalUser));
			}

			return false;
		}

		private bool CanSync()
		{
			// We do not want to send actor updates until we're fully joined to the app.
			// TODO: We shouldn't need to do this check. The engine shouldn't try to send
			// updates until we're fully joined to the app.
			if (!(App.Protocol is Messaging.Protocols.Execution))
			{
				return false;
			}

			Attachment attachmentInHierarchy = FindAttachmentInHierarchy();
			bool inAttachmentHeirarchy = (attachmentInHierarchy != null);
			bool inOwnedAttachmentHierarchy = (inAttachmentHeirarchy && LocalUser != null && attachmentInHierarchy.UserId == LocalUser.Id);

			// We can send actor updates to the app if we're operating in a server-authoritative model,
			// or if we're in a peer-authoritative model and we've been designated the authoritative peer.
			// Override the previous rules if this actor is grabbed by the local user or is in an attachment
			// hierarchy owned by the local user.
			if (App.OperatingModel == OperatingModel.ServerAuthoritative ||
				(RigidBody3D == null &&
					(App.IsAuthoritativePeer ||
					IsGrabbed ||
					_grabbedLastSync ||
					inOwnedAttachmentHierarchy)) ||
				(RigidBody3D != null &&
					(IsSimulatedByLocalUser || IsGrabbed ||
					_grabbedLastSync )))
			{
				return true;
			}

			return false;
		}

		#endregion

		#region Command Handlers

		[CommandHandler(typeof(LocalCommand))]
		private void OnLocalCommand(LocalCommand payload, Action onCompleteCallback)
		{
			payload.Command?.Invoke();
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(ActorCorrection))]
		private void OnActorCorrection(ActorCorrection payload, Action onCompleteCallback)
		{
			EngineCorrection(payload);
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(ActorUpdate))]
		private void OnActorUpdate(ActorUpdate payload, Action onCompleteCallback)
		{
			SynchronizeEngine(payload.Actor);
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(RigidBodyCommands))]
		private void OnRigidBody3DCommands(RigidBodyCommands payload, Action onCompleteCallback)
		{
			ExecuteRigidBody3DCommands(payload, onCompleteCallback);
		}

		[CommandHandler(typeof(SetMediaState))]
		private void OnSetMediaState(SetMediaState payload, Action onCompleteCallback)
		{
			if (_mediaInstances == null)
			{
				_mediaInstances = new Dictionary<Guid, MediaInstance>();
			}
			switch (payload.MediaCommand)
			{
				case MediaCommand.Start:
					{
						MediaInstance mediaInstance = new MediaInstance(payload.MediaAssetId);
						_mediaInstances.Add(payload.Id, mediaInstance);

						App.AssetManager.OnSet(payload.MediaAssetId, asset =>
						{
							if (asset.Asset is AudioStream audioClip)
							{
								AudioStreamPlayer3D soundInstance = App.SoundManager.AddSoundInstance(this, payload.Id, audioClip, payload.Options);
								if (soundInstance != null)
								{
									mediaInstance.Instance = soundInstance;
								}
								else
								{
									App.Logger.LogError($"Trying to start sound instance that should already have completed for: {payload.MediaAssetId}\n");
									_mediaInstances.Remove(payload.Id);
								}
							}
							/*FIXME
							else if (asset.Asset is VideoStreamDescription videoStreamDescription)
							{
								var factory = MREAPI.AppsAPI.VideoPlayerFactory
									?? throw new ArgumentException("Cannot start video stream - VideoPlayerFactory not implemented.");
								IVideoPlayer videoPlayer = factory.CreateVideoPlayer(this);
								videoPlayer.Play(videoStreamDescription, payload.Options);
								mediaInstance.Instance = videoPlayer;
							}
							*/
							else
							{
								App.Logger.LogError($"Failed to start media instance with asset id: {payload.MediaAssetId}\n");
								_mediaInstances.Remove(payload.Id);
							}
						});
					}
					break;
				case MediaCommand.Stop:
					{
						if (_mediaInstances.TryGetValue(payload.Id, out MediaInstance mediaInstance))
						{
							App.AssetManager.OnSet(mediaInstance.MediaAssetId, _ =>
							{
								_mediaInstances.Remove(payload.Id);
								DestroyMediaById(payload.Id, mediaInstance);
							});
						}
					}
					break;
				case MediaCommand.Update:
					{
						if (_mediaInstances.TryGetValue(payload.Id, out MediaInstance mediaInstance))
						{
							App.AssetManager.OnSet(mediaInstance.MediaAssetId, _ =>
							{
								if (mediaInstance.Instance != null)
								{
									if (mediaInstance.Instance is AudioStreamPlayer3D soundInstance)
									{
										App.SoundManager.ApplyMediaStateOptions(this, soundInstance, payload.Options, payload.Id, false);
									}
									/*FIXME
									else if (mediaInstance.Instance is IVideoPlayer videoPlayer)
									{
										videoPlayer.ApplyMediaStateOptions(payload.Options);
									}
									*/
								}
							});
						}
					}
					break;
			}
			onCompleteCallback?.Invoke();
		}

		public bool CheckIfSoundExpired(Guid id)
		{
			if (_mediaInstances != null && _mediaInstances.TryGetValue(id, out MediaInstance mediaInstance))
			{
				if (mediaInstance.Instance != null)
				{
					if (mediaInstance.Instance is AudioStreamPlayer3D soundInstance)
					{
						if (soundInstance.Playing)
						{
							return false;
						}
						DestroyMediaById(id, mediaInstance);
					}
				}
			}
			return true;
		}

		private void DestroyMediaById(Guid id, MediaInstance mediaInstance)
		{
			if (mediaInstance.Instance != null)
			{
				if (mediaInstance.Instance is AudioStreamPlayer3D soundInstance)
				{
					App.SoundManager.DestroySoundInstance(soundInstance, id);
				}
				/*FXIME
				else if (mediaInstance.Instance is IVideoPlayer videoPlayer)
				{
					videoPlayer.Destroy();
				}
				*/
				mediaInstance.Instance = null;
			}
		}

		[CommandHandler(typeof(InterpolateActor))]
		private void OnInterpolateActor(InterpolateActor payload, Action onCompleteCallback)
		{
			GetOrCreateActorComponent<AnimationComponent>()
				.Interpolate(
					payload.Value,
					payload.AnimationName,
					payload.Duration,
					payload.Curve,
					payload.Enabled);
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(SetBehavior))]
		private void OnSetBehavior(SetBehavior payload, Action onCompleteCallback)
		{
			// Don't create a behavior at all for this actor if the app is not interactable for any users.
			if (!App.InteractionEnabled())
			{
				onCompleteCallback?.Invoke();
				return;
			}

			var behaviorComponent = GetOrCreateActorComponent<BehaviorComponent>();

			if (behaviorComponent.ContainsBehaviorContext())
			{
				behaviorComponent.ClearBehaviorContext();
			}

			if (payload.BehaviorType != BehaviorType.None)
			{
				var context = BehaviorContextFactory.CreateContext(payload.BehaviorType, this, new WeakReference<MixedRealityExtensionApp>(App));

				if (context == null)
				{
					GD.PushError($"Failed to create behavior for behavior type {payload.BehaviorType.ToString()}");
					onCompleteCallback?.Invoke();
					return;
				}

				behaviorComponent.SetBehaviorContext(context);

				// We need to update the new behavior's grabbable flag from the actor so that it can be grabbed in the case we cleared the previous behavior.
				((ITargetBehavior)context.Behavior).Grabbable = Grabbable;
				((ITargetBehavior)context.Behavior).Touchable = Touchable;
			}

			onCompleteCallback?.Invoke();
		}

		#endregion

		#region Command Handlers - Rigid Body Commands
/*FIXME
		[CommandHandler(typeof(RBMovePosition))]
		private void OnRBMovePosition(RBMovePosition payload, Action onCompleteCallback)
		{
			RigidBody3D?.RigidBody3DMovePosition(new MWVector3().ApplyPatch(payload.Position));
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(RBMoveRotation))]
		private void OnRBMoveRotation(RBMoveRotation payload, Action onCompleteCallback)
		{
			RigidBody3D?.RigidBody3DMoveRotation(new MWQuaternion().ApplyPatch(payload.Rotation));
			onCompleteCallback?.Invoke();
		}
*/
		[CommandHandler(typeof(RBAddForce))]
		private void OnRBAddForce(RBAddForce payload, Action onCompleteCallback)
		{
			bool isOwner = Owner.HasValue ? Owner.Value == App.LocalUser.Id : CanSync();
			if (isOwner)
			{
				payload.Force.Z *= -1;
				RigidBody3D?.RigidBody3DAddForce(new MWVector3().ApplyPatch(payload.Force));
			}

			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(RBAddForceAtPosition))]
		private void OnRBAddForceAtPosition(RBAddForceAtPosition payload, Action onCompleteCallback)
		{
			var force = new MWVector3().ApplyPatch(payload.Force);
			var position = new MWVector3().ApplyPatch(payload.Position);
			RigidBody3D?.RigidBody3DAddForceAtPosition(force, position);
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(RBAddTorque))]
		private void OnRBAddTorque(RBAddTorque payload, Action onCompleteCallback)
		{
			payload.Torque.X *= -1;
			payload.Torque.Y *= -1;
			RigidBody3D?.RigidBody3DAddTorque(new MWVector3().ApplyPatch(payload.Torque));
			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(RBAddRelativeTorque))]
		private void OnRBAddRelativeTorque(RBAddRelativeTorque payload, Action onCompleteCallback)
		{
			payload.RelativeTorque.X *= -1;
			payload.RelativeTorque.Y *= -1;
			RigidBody3D?.RigidBody3DAddRelativeTorque(new MWVector3().ApplyPatch(payload.RelativeTorque));
			onCompleteCallback?.Invoke();
		}

		#endregion
	}
}
