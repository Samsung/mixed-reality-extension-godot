// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.Animation;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using MixedRealityExtension.Util.GodotHelper;

using Godot;

using GodotAnimation = Godot.Animation;

namespace MixedRealityExtension.Core.Components
{
	internal class AnimationComponent : ActorComponentBase
	{
		private class AnimationData
		{
			public bool Enabled;
			public bool IsInternal;
			public bool Managed;
		}
		private Dictionary<string, AnimationPlayer> _animationPlayers = new Dictionary<string, AnimationPlayer>();
		private Dictionary<string, AnimationData> _animationData = new Dictionary<string, AnimationData>();

		private bool GetAnimationData(string animationName, out AnimationData animationData) => _animationData.TryGetValue(animationName, out animationData);

		public override void _Process(float delta)
		{
			// Check for changes to an animation's enabled state and notify the server when a change is detected.
			if (_animationPlayers.Count == 0) return;

			foreach (var animationPlayer in _animationPlayers.Values)
			{
				if (!GetAnimationData(animationPlayer.Name, out AnimationData animationData)
					|| animationData.Managed
					|| animationData.Enabled == animationPlayer.PlaybackActive
				)
					continue;
				
				animationData.Enabled = animationPlayer.PlaybackActive;

				// Let the app know this animation (or interpolation) changed state.
				NotifySetAnimationStateEvent(
					animationPlayer.Name,
					animationTime: null,
					animationSpeed: null,
					animationEnabled: animationData.Enabled);

				// If the animation stopped, sync the actor's final transform.
				if (!animationData.Enabled)
				{
					AttachedActor.SynchronizeApp(ActorComponentType.Transform);
				}

				// If this was an internal one-shot animation (aka an interpolation), remove it.
				if (!animationData.Enabled && animationData.IsInternal)
				{
					_animationData.Remove(animationPlayer.Name);
					GD.Print(animationPlayer.Name);
					this.RemoveChild(animationPlayer);
					animationPlayer.QueueFree();
					_animationPlayers.Remove(animationPlayer.Name);
				}
			}
		}

		internal void CreateAnimation(
			string animationName,
			IEnumerable<MWAnimationKeyframe> keyframes,
			IEnumerable<MWAnimationEvent> events,
			MWAnimationWrapMode wrapMode,
			MWSetAnimationStateOptions initialState,
			bool isInternal,
			bool managed,
			Action onCreatedCallback)
		{
			var continuation = new MWContinuation(AttachedActor, null, (result) =>
			{
				var animationPlayer = GetOrCreateGodotAnimationPlayer(animationName);
				var animation = new GodotAnimation();
				animation.Loop = wrapMode.IsInterpolationLoopWrap();

				var curves = new Dictionary<string, int>();

				int GetOrCreateCurve(Type type, string propertyName)
				{
					if (!curves.TryGetValue(propertyName, out int trackIndex))
					{
						trackIndex = animation.AddTrack(GodotAnimation.TrackType.Bezier);
						curves.Add(propertyName, trackIndex);
					}

					return trackIndex;
				}

				void AddFloatPatch(Type type, string propertyName, float time, float? value)
				{
					if (value.HasValue)
					{
						var trackIndex = GetOrCreateCurve(type, propertyName);
						animation.TrackSetPath(trackIndex, propertyName);
						animation.BezierTrackInsertKey(trackIndex, time, value.Value);
					}
				}

				void AddVector3Patch(Type type, string propertyName, float time, Vector3Patch value)
				{
					AddFloatPatch(type, String.Format("{0}:x", propertyName), time, value?.X);
					AddFloatPatch(type, String.Format("{0}:y", propertyName), time, value?.Y);
					AddFloatPatch(type, String.Format("{0}:z", propertyName), time, value?.Z);
				}

				void AddQuaternionPatch(Type type, string propertyName, float time, QuaternionPatch value)
				{
					AddFloatPatch(type, String.Format("{0}:x", propertyName), time, value?.X);
					AddFloatPatch(type, String.Format("{0}:y", propertyName), time, value?.Y);
					AddFloatPatch(type, String.Format("{0}:z", propertyName), time, value?.Z);
					AddFloatPatch(type, String.Format("{0}:w", propertyName), time, value?.W);
				}

				void AddTransformPatch(float time, ScaledTransformPatch value)
				{
					// Work around a Unity bug/feature where all position components must be specified
					// in the keyframe or the missing fields get set to zero.
					Vector3Patch position = value?.Position;
					if (position != null && position.IsPatched())
					{
						if (!position.X.HasValue) { position.X = Transform.origin.x; }
						if (!position.Y.HasValue) { position.Y = Transform.origin.y; }
						if (!position.Z.HasValue) { position.Z = Transform.origin.z; }
					}
					// Work around a Unity bug/feature where all scale components must be specified
					// in the keyframe or the missing fields get set to one.
					Vector3Patch scale = value?.Scale;
					if (scale != null && scale.IsPatched())
					{
						if (!scale.X.HasValue) { scale.X = Scale.x; }
						if (!scale.Y.HasValue) { scale.Y = Scale.y; }
						if (!scale.Z.HasValue) { scale.Z = Scale.z; }
					}
					AddVector3Patch(typeof(Transform), "..:translation", time, value?.Position);
					var ratation = value?.Rotation;
					var quat = new Quat(ratation.X.Value, ratation.Y.Value, ratation.Z.Value, ratation.W.Value);
					var vector3 = quat.GetEuler();
					AddVector3Patch(typeof(Transform), "..:rotation_degrees", time, new Vector3Patch(vector3));
					AddVector3Patch(typeof(Transform), "..:scale", time, value?.Scale);
				}

				void AddActorPatch(float time, ActorPatch value)
				{
					AddTransformPatch(time, value?.Transform.Local);
				}

				void AddKeyframe(MWAnimationKeyframe keyframe)
				{
					AddActorPatch(keyframe.Time, keyframe.Value);
					if (animation.Length < keyframe.Time)
						animation.Length = keyframe.Time;
				}

				foreach (var keyframe in keyframes)
				{
					AddKeyframe(keyframe);
				}

				_animationData[animationName] = new AnimationData()
				{
					IsInternal = isInternal,
					Managed = managed
				};

				float initialTime = 0f;
				float initialSpeed = 1f;
				bool initialEnabled = false;

				if (initialState != null)
				{
					initialTime = initialState.Time ?? initialTime;
					initialSpeed = initialState.Speed ?? initialSpeed;
					initialEnabled = initialState.Enabled ?? initialEnabled;
				}

				animationPlayer.AddAnimation(animationName, animation);
				animationPlayer.AssignedAnimation = animationName;

				SetAnimationState(animationName, initialTime, initialSpeed, initialEnabled);

				onCreatedCallback?.Invoke();
			});

			continuation.Start();
		}

		internal void Interpolate(
			ActorPatch finalFrame,
			string animationName,
			float duration,
			float[] curve,
			bool enabled)
		{
			// Ensure duration is in range [0...n].
			duration = Math.Max(0, duration);

			const int FPS = 10;
			float timeStep = duration / FPS;

			// If the curve is malformed, fall back to linear.
			if (curve.Length != 4)
			{
				curve = new float[] { 0, 0, 1, 1 };
			}

			// Are we patching the transform?
			bool animateTransform = finalFrame.Transform != null && finalFrame.Transform.Local != null && finalFrame.Transform.Local.IsPatched();
			var finalTransform = finalFrame.Transform.Local;

			// What parts of the transform are we animating?
			bool animatePosition = animateTransform && finalTransform.Position != null && finalTransform.Position.IsPatched();
			bool animateRotation = animateTransform && finalTransform.Rotation != null && finalTransform.Rotation.IsPatched();
			bool animateScale = animateTransform && finalTransform.Scale != null && finalTransform.Scale.IsPatched();

			// Ensure we have a well-formed rotation quaternion.
			for (; animateRotation;)
			{
				var rotation = finalTransform.Rotation;
				bool hasAllComponents =
					rotation.X.HasValue &&
					rotation.Y.HasValue &&
					rotation.Z.HasValue &&
					rotation.W.HasValue;

				// If quaternion is incomplete, fall back to the identity.
				if (!hasAllComponents)
				{
					finalTransform.Rotation = new QuaternionPatch(Quat.Identity);
					break;
				}

				// Ensure the quaternion is normalized.
				var lengthSquared =
					(rotation.X.Value * rotation.X.Value) +
					(rotation.Y.Value * rotation.Y.Value) +
					(rotation.Z.Value * rotation.Z.Value) +
					(rotation.W.Value * rotation.W.Value);
				if (lengthSquared == 0)
				{
					// If the quaternion is length zero, fall back to the identity.
					finalTransform.Rotation = new QuaternionPatch(Quat.Identity);
					break;
				}
				else if (lengthSquared != 1.0f)
				{
					// If the quaternion length is not 1, normalize it.
					var inverseLength = 1.0f / Mathf.Sqrt(lengthSquared);
					rotation.X *= inverseLength;
					rotation.Y *= inverseLength;
					rotation.Z *= inverseLength;
					rotation.W *= inverseLength;
				}
				break;
			}

			// Create the sampler to calculate ease curve values.
			var sampler = new CubicBezier(curve[0], curve[1], curve[2], curve[3]);

			var keyframes = new List<MWAnimationKeyframe>();

			// Generate keyframes
			float currTime = 0;

			do
			{
				var keyframe = NewKeyframe(currTime);
				var unitTime = duration > 0 ? currTime / duration : 1;
				BuildKeyframe(keyframe, unitTime);
				keyframes.Add(keyframe);
				currTime += timeStep;
			}
			while (currTime <= duration && timeStep > 0);

			// Final frame (if needed)
			if (currTime - duration > 0)
			{
				var keyframe = NewKeyframe(duration);
				BuildKeyframe(keyframe, 1);
				keyframes.Add(keyframe);
			}

			// Create and optionally start the animation.
			CreateAnimation(
				animationName,
				keyframes,
				events: null,
				wrapMode: MWAnimationWrapMode.Once,
				initialState: new MWSetAnimationStateOptions { Enabled = enabled },
				isInternal: true,
				managed: false,
				onCreatedCallback: null);

			bool LerpFloat(out float dest, float start, float? end, float t)
			{
				if (end.HasValue)
				{
					dest = Mathf.Lerp(start, end.Value, t);
					return true;
				}
				dest = 0;
				return false;
			}

			bool SlerpQuaternion(out Quat dest, Quat start, QuaternionPatch end, float t)
			{
				if (end != null)
				{
					dest = start.Slerp(new Quat(end.X.Value, end.Y.Value, end.Z.Value, end.W.Value), t);
					return true;
				}
				dest = Quat.Identity;
				return false;
			}

			void BuildKeyframePosition(MWAnimationKeyframe keyframe, float t)
			{
				float value;
				if (LerpFloat(out value, Transform.origin.x, finalTransform.Position.X, t))
				{
					keyframe.Value.Transform.Local.Position.X = value;
				}
				if (LerpFloat(out value, Transform.origin.y, finalTransform.Position.Y, t))
				{
					keyframe.Value.Transform.Local.Position.Y = value;
				}
				if (LerpFloat(out value, Transform.origin.z, finalTransform.Position.Z, t))
				{
					keyframe.Value.Transform.Local.Position.Z = value;
				}
			}

			void BuildKeyframeScale(MWAnimationKeyframe keyframe, float t)
			{
				float value;
				if (LerpFloat(out value, Scale.x, finalTransform.Scale.X, t))
				{
					keyframe.Value.Transform.Local.Scale.X = value;
				}
				if (LerpFloat(out value, Scale.y, finalTransform.Scale.Y, t))
				{
					keyframe.Value.Transform.Local.Scale.Y = value;
				}
				if (LerpFloat(out value, Scale.z, finalTransform.Scale.Z, t))
				{
					keyframe.Value.Transform.Local.Scale.Z = value;
				}
			}

			void BuildKeyframeRotation(MWAnimationKeyframe keyframe, float t)
			{
				Quat value;
				if (SlerpQuaternion(out value, new Quat(Rotation), finalTransform.Rotation, t))
				{
					keyframe.Value.Transform.Local.Rotation = new QuaternionPatch(value);
				}
			}

			void BuildKeyframe(MWAnimationKeyframe keyframe, float unitTime)
			{
				float curveTime = sampler.Sample(unitTime);

				if (animatePosition)
				{
					BuildKeyframePosition(keyframe, curveTime);
				}
				if (animateRotation)
				{
					BuildKeyframeRotation(keyframe, curveTime);
				}
				if (animateScale)
				{
					BuildKeyframeScale(keyframe, curveTime);
				}
			}

			MWAnimationKeyframe NewKeyframe(float time)
			{
				var keyframe = new MWAnimationKeyframe
				{
					Time = time,
					Value = new ActorPatch()
				};

				if (animateTransform)
				{
					keyframe.Value.Transform = new ActorTransformPatch()
					{
						Local = new ScaledTransformPatch()
					};
				}
				if (animatePosition)
				{
					keyframe.Value.Transform.Local.Position = new Vector3Patch();
				}
				if (animateRotation)
				{
					keyframe.Value.Transform.Local.Rotation = new QuaternionPatch();
				}
				if (animateScale)
				{
					keyframe.Value.Transform.Local.Scale = new Vector3Patch();
				}
				return keyframe;
			}
		}

		internal void SetAnimationState(string animationName, float? time, float? speed, bool? enabled)
		{
			var animationPlayer = GetOrCreateGodotAnimationPlayer(animationName);
			if (animationPlayer != null)
			{
				var animation = animationPlayer.GetAnimation(animationName);
				if (animation != null)
				{
					// Create the animationData if it doesn't already exist. This is the case for gltf animations.
					if (!GetAnimationData(animationName, out AnimationData animationData))
					{
						_animationData[animationName] = animationData = new AnimationData();
					}

					if (speed.HasValue)
					{
						animationPlayer.PlaybackSpeed = speed.Value;
					}
					if (time.HasValue)
					{
						SetAnimationTime(animationPlayer, time.Value);
					}
					if (enabled.HasValue)
					{
						EnableAnimation(animationName, enabled.Value);
					}
				}
			}
		}

		private void EnableAnimation(string animationName, bool? enabled)
		{
			if (enabled.HasValue)
			{
				var animationPlayer = GetGodotAnimationPlayer(animationName);
				if (animationPlayer != null)
				{
					var animation = animationPlayer.GetAnimation(animationName);
					var wasEnabled = animationPlayer.PlaybackActive;
					if (wasEnabled != enabled.Value)
					{
						animationPlayer.PlaybackActive = enabled.Value;
						// NOTE: animationData.Enabled will be set in the next call to Update()

						// When stopping an animation, send an update to the app letting it know the final animation state.
						if (!enabled.Value && (AttachedActor.App.IsAuthoritativePeer || AttachedActor.App.OperatingModel == OperatingModel.ServerAuthoritative))
						{
							// FUTURE: Add additional animatable properties as support for them is added (light color, etc).
							AttachedActor.SendActorUpdate(ActorComponentType.Transform);
						}
					}
					if (enabled.Value)
						animationPlayer.Play();
				}
			}
		}

		internal IList<MWActorAnimationState> GetAnimationStates()
		{
			var animationStates = new List<MWActorAnimationState>();
			foreach (var animationPlayer in _animationPlayers.Values)
			{
				// don't report sync state of managed animations here
				if (_animationData.TryGetValue(animationPlayer.Name, out var data) && data.Managed)
				{
					continue;
				}

				animationStates.Add(GetAnimationState(animationPlayer));

				if (animationStates.Count > 0)
				{
					return animationStates;
				}
			}

			return null;
		}

		MWActorAnimationState GetAnimationState(AnimationPlayer animationPlayer)
		{
			return new MWActorAnimationState()
			{
				ActorId = this.AttachedActor.Id,
				AnimationName = animationPlayer.Name,
				State = new MWSetAnimationStateOptions
				{
					Time = animationPlayer.CurrentAnimationPosition,
					Speed = animationPlayer.PlaybackSpeed,
					Enabled = animationPlayer.PlaybackActive
				}
			};
		}

		internal void ApplyAnimationState(MWActorAnimationState animationState)
		{
			SetAnimationState(animationState.AnimationName, animationState.State.Time, animationState.State.Speed, animationState.State.Enabled);
		}

		private void NotifySetAnimationStateEvent(string animationName, float? animationTime, float? animationSpeed, bool? animationEnabled)
		{
			AttachedActor.App.EventManager.QueueEvent(new SetAnimationStateEvent(AttachedActor.Id, animationName, animationTime, animationSpeed, animationEnabled));
		}

		private void SetAnimationTime(AnimationPlayer animationPlayer, float animationTime)
		{
			if (animationTime < 0)
			{
				animationTime = animationPlayer.CurrentAnimationLength;
			}

			animationPlayer.Seek(animationTime);
		}

		private AnimationPlayer GetOrCreateGodotAnimationPlayer(string animationName)
		{
			if (_animationPlayers.TryGetValue(animationName, out AnimationPlayer animationPlayer))
			{
				return animationPlayer;
			}
			else
			{
				animationPlayer = new AnimationPlayer() { Name = animationName };
				this.AddChild(animationPlayer);
				_animationPlayers.Add(animationName, animationPlayer);
				return animationPlayer;
			}
		}

		private AnimationPlayer GetGodotAnimationPlayer(string animationName)
		{
			return _animationPlayers[animationName];
		}
	}
}
