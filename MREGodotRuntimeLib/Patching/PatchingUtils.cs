// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching.Types;
using Godot;
using System;

using MRECollisionDetectionMode = MixedRealityExtension.Core.Interfaces.CollisionDetectionMode;
using MRELightType = MixedRealityExtension.Core.Interfaces.LightType;
using GodotLightType = Godot.RenderingServer.LightType;
using MixedRealityExtension.Util.GodotHelper;

namespace MixedRealityExtension.Patching
{
	internal static class PatchingUtilMethods
	{
		public static T? GeneratePatch<T>(T _old, T _new) where T : struct
		{
			T? ret = null;
			if (!_old.Equals(_new))
			{
				ret = _new;
			}

			return ret;
		}

		public static T[] GeneratePatch<T>(T[] _old, T[] _new) where T : struct
		{
			if ((_old == null && _new != null) || _new != null)
			{
				return _new;
			}
			else
			{
				return null;
			}
		}

		public static string GeneratePatch(string _old, string _new)
		{
			if (_old == null && _new != null)
			{
				return _new;
			}
			else if (_new == null)
			{
				return null;
			}
			if (_old != _new)
			{
				return _new;
			}
			else
			{
				return null;
			}
		}

		public static Vector3Patch GeneratePatch(MWVector3 _old, Vector3 _new)
		{
			if (_old == null && _new != null)
			{
				return new Vector3Patch(_new);
			}
			else if (_new == null)
			{
				return null;
			}

			var patch = new Vector3Patch()
			{
				X = _old.X != _new.x ? (float?)_new.x : null,
				Y = _old.Y != _new.y ? (float?)_new.y : null,
				Z = _old.Z != _new.z ? (float?)_new.z : null
			};

			if (patch.IsPatched())
			{
				return patch;
			}
			else
			{
				return null;
			}
		}

		public static QuaternionPatch GeneratePatch(MWQuaternion _old, Quaternion _new)
		{
			if (_old == null && _new != null)
			{
				return new QuaternionPatch(_new);
			}
			else if (_new == null)
			{
				return null;
			}

			var patch = new QuaternionPatch()
			{
				X = Math.Abs(_old.X - _new.x) > 0.001f ? (float?)_new.x : null,
				Y = Math.Abs(_old.Y - _new.y) > 0.001f ? (float?)_new.y : null,
				Z = Math.Abs(_old.Z - _new.z) > 0.001f ? (float?)_new.z : null,
				W = Math.Abs(_old.W - _new.w) > 0.001f ? (float?)_new.w : null
			};

			if (patch.IsPatched())
			{
				return patch;
			}
			else
			{
				return null;
			}
		}

		public static TransformPatch GenerateAppTransformPatch(MWTransform _old, Node3D _new, Node3D appRoot)
		{
			var globalTransform = appRoot.GlobalTransform.AffineInverse() * _new.GlobalTransform;
			var globalOrigin = globalTransform.origin;
			globalOrigin.z *= -1;
			var globalRotation = globalTransform.basis.GetRotationQuaternion();
			globalRotation.x *= -1;
			globalRotation.y *= -1;

			TransformPatch transform = new TransformPatch()
			{
				Position = GeneratePatch(_old?.Position, globalOrigin),
				Rotation = GeneratePatch(_old?.Rotation, globalRotation),
			};

			return transform.IsPatched() ? transform : null;
		}

		public static ScaledTransformPatch GenerateLocalTransformPatch(MWScaledTransform _old, Node3D _new)
		{
			var position = _new.Transform.origin;
			position.z *= -1;
			var rotation = _new.Transform.basis.GetRotationQuaternion();
			rotation.x *= -1;
			rotation.y *= -1;


			ScaledTransformPatch transform = new ScaledTransformPatch()
			{
				Position = GeneratePatch(_old.Position, position),
				Rotation = GeneratePatch(_old.Rotation, rotation),
				Scale = GeneratePatch(_old.Scale, _new.Scale)
			};

			return transform.IsPatched() ? transform : null;
		}

		public static ColorPatch GeneratePatch(MWColor _old, Color _new)
		{
			if (_old == null && _new != null)
			{
				return new ColorPatch(_new);
			}
			else if (_new == null)
			{
				return null;
			}

			var patch = new ColorPatch()
			{
				R = _old.R != _new.r ? (float?)_new.r : null,
				G = _old.G != _new.g ? (float?)_new.g : null,
				B = _old.B != _new.b ? (float?)_new.b : null,
				A = _old.A != _new.a ? (float?)_new.a : null
			};

			if (patch.IsPatched())
			{
				return patch;
			}
			else
			{
				return null;
			}
		}

		public static RigidBodyPatch GeneratePatch(MixedRealityExtension.Core.RigidBody _old, Godot.RigidBody3D _new,
			Node3D sceneRoot, bool addVelocities)
		{
			if (_old == null && _new != null)
			{
				return new RigidBodyPatch(_new, sceneRoot);
			}
			else if (_new == null)
			{
				return null;
			}

			var patch = new RigidBodyPatch()
			{
				// Do not include Position or Rotation in the patch.

				// we add velocities only if there is an explicit subscription for it, since it might cause significant bandwidth
				Velocity = ((addVelocities) ?
				  GeneratePatch(_old.Velocity, sceneRoot.ToLocal(_new.LinearVelocity)) : null),
				AngularVelocity = ((addVelocities) ?
				  GeneratePatch(_old.AngularVelocity, sceneRoot.ToLocal(_new.AngularVelocity)) : null),

				CollisionDetectionMode = GeneratePatch(
					_old.CollisionDetectionMode,
					_new.ContinuousCd switch
					{
						true => MRECollisionDetectionMode.Continuous,
						false => MRECollisionDetectionMode.Discrete
					}),
				ConstraintFlags = GeneratePatch(_old.ConstraintFlags, _new.GetMRERigidBodyConstraints()),
				DetectCollisions = GeneratePatch(_old.DetectCollisions, !_new.GetChild<CollisionShape3D>()?.Disabled ?? false),
				Mass = GeneratePatch(_old.Mass, _new.Mass),
				UseGravity = GeneratePatch(_old.UseGravity, !Mathf.IsZeroApprox(_new.GravityScale)),
			};

			if (patch.IsPatched())
			{
				return patch;
			}
			else
			{
				return null;
			}
		}
		/*FIXME
		public static LightPatch GeneratePatch(MRELight _old, UnityLight _new)
		{
			if (_old == null && _new != null)
			{
				return new LightPatch(_new);
			}
			else if (_new == null)
			{
				return null;
			}

			var patch = new LightPatch()
			{
				Enabled = _new.enabled,
				Type = UtilMethods.ConvertEnum<Core.Interfaces.LightType, UnityEngine.LightType>(_new.type),
				Color = new ColorPatch(_new.color),
				Range = _new.range,
				Intensity = _new.intensity,
				SpotAngle = _new.spotAngle
			};

			if (patch.IsPatched())
			{
				return patch;
			}
			else
			{
				return null;
			}
		}
		*/
	}

	public static class PatchingUtilsExtensions
	{
		public static T ApplyPatch<T>(this T _this, T? patch) where T : struct
		{
			if (patch.HasValue)
			{
				_this = patch.Value;
			}

			return _this;
		}

		public static string ApplyPatch(this string _this, string patch)
		{
			if (patch != null)
			{
				_this = patch;
			}

			return _this;
		}

		public static MWVector2 ApplyPatch(this MWVector2 _this, Vector2Patch vector)
		{
			if (vector == null)
			{
				return _this;
			}

			if (vector.X != null)
			{
				_this.X = vector.X.Value;
			}

			if (vector.Y != null)
			{
				_this.Y = vector.Y.Value;
			}

			return _this;
		}

		public static MWVector3 ApplyPatch(this MWVector3 _this, Vector3Patch vector)
		{
			if (vector == null)
			{
				return _this;
			}

			if (vector.X != null)
			{
				_this.X = vector.X.Value;
			}

			if (vector.Y != null)
			{
				_this.Y = vector.Y.Value;
			}

			if (vector.Z != null)
			{
				_this.Z = vector.Z.Value;
			}

			return _this;
		}

		public static MWQuaternion ApplyPatch(this MWQuaternion _this, QuaternionPatch quaternion)
		{
			if (quaternion == null)
			{
				return _this;
			}

			if (quaternion.W != null)
			{
				_this.W = quaternion.W.Value;
			}

			if (quaternion.X != null)
			{
				_this.X = quaternion.X.Value;
			}

			if (quaternion.Y != null)
			{
				_this.Y = quaternion.Y.Value;
			}

			if (quaternion.Z != null)
			{
				_this.Z = quaternion.Z.Value;
			}

			return _this;
		}

		public static MWColor ApplyPatch(this MWColor _this, ColorPatch color)
		{
			if (color == null)
			{
				return _this;
			}

			if (color.A != null)
			{
				_this.A = color.A.Value;
			}

			if (color.R != null)
			{
				_this.R = color.R.Value;
			}

			if (color.G != null)
			{
				_this.G = color.G.Value;
			}

			if (color.B != null)
			{
				_this.B = color.B.Value;
			}

			return _this;
		}

		public static void ApplyLocalPatch(this Node3D _this, MWScaledTransform current, ScaledTransformPatch patch)
		{
			var localPosition = _this.Transform.origin;
			var localRotation = _this.Transform.basis.GetRotationQuaternion();
			var localScale = _this.Scale;

			if (patch.Position != null)
			{
				localPosition = localPosition.GetPatchApplied(current.Position.ApplyPatch(patch.Position));
				localPosition.z *= -1;
			}

			if (patch.Rotation != null)
			{
				localRotation = localRotation.GetPatchApplied(current.Rotation.ApplyPatch(patch.Rotation));
				localRotation.x *= -1;
				localRotation.y *= -1;
			}

			if (patch.Scale != null)
			{
				localScale = localScale.GetPatchApplied(current.Scale.ApplyPatch(patch.Scale));
			}

			var basis = new Basis(localRotation);
			basis.Scale = localScale;
			_this.Transform = new Transform3D(basis, localPosition);
		}

		public static void ApplyAppPatch(this Node3D _this, Node3D appRoot, MWTransform current, TransformPatch patch)
		{
			var globalPosition = _this.GlobalTransform.origin;
			var globalRotation = _this.GlobalTransform.basis.GetRotationQuaternion();
			var globalScale = _this.GlobalTransform.basis.Scale;

			if (patch.Position != null)
			{
				globalPosition = appRoot.ToLocal(_this.GlobalTransform.origin).GetPatchApplied(current.Position.ApplyPatch(patch.Position));
				globalPosition.z *= -1;
				globalPosition = appRoot.ToGlobal(globalPosition);
			}

			if (patch.Rotation != null)
			{
				var appRotation = appRoot.GlobalTransform.basis.GetRotationQuaternion();
				var currAppRotation = appRotation.Inverse() * globalRotation;
				var newAppRotation = currAppRotation.GetPatchApplied(current.Rotation.ApplyPatch(patch.Rotation));
				newAppRotation.x *= -1;
				newAppRotation.y *= -1;
				globalRotation = appRotation * newAppRotation;
			}

			var basis = new Basis(globalRotation);
			basis.Scale = globalScale;
			_this.GlobalTransform = new Transform3D(basis, globalPosition);
		}

		public static T GetPatchApplied<T>(this T _this, T value) where T : struct
		{

			if (!_this.Equals(value))
			{
				_this = value;
			}

			return _this;
		}

		public static string GetPatchApplied(this string _this, string value)
		{
			if (!_this.Equals(value))
			{
				_this = value;
			}

			return _this;
		}

		public static Vector2 GetPatchApplied(this Vector2 _this, MWVector2 vector)
		{
			_this.x = _this.x.GetPatchApplied(vector.X);
			_this.y = _this.y.GetPatchApplied(vector.Y);

			return _this;
		}

		public static Vector3 GetPatchApplied(this Vector3 _this, MWVector3 vector)
		{
			_this.x = _this.x.GetPatchApplied(vector.X);
			_this.y = _this.y.GetPatchApplied(vector.Y);
			_this.z = _this.z.GetPatchApplied(vector.Z);

			return _this;
		}

		public static Quaternion GetPatchApplied(this Quaternion _this, MWQuaternion quaternion)
		{
			_this.w = _this.w.GetPatchApplied(quaternion.W);
			_this.x = _this.x.GetPatchApplied(quaternion.X);
			_this.y = _this.y.GetPatchApplied(quaternion.Y);
			_this.z = _this.z.GetPatchApplied(quaternion.Z);

			return _this;
		}

		public static Color GetPatchApplied(this Color _this, MWColor color)
		{
			_this.r = _this.r.GetPatchApplied(color.R);
			_this.g = _this.g.GetPatchApplied(color.G);
			_this.b = _this.b.GetPatchApplied(color.B);
			_this.a = _this.a.GetPatchApplied(color.A);

			return _this;
		}

		public static GodotLightType GetPatchApplied(this RenderingServer.LightType _this, MRELightType value)
		{
			var lightType = (GodotLightType)Enum.Parse(typeof(GodotLightType), value.ToString());
			if (!_this.Equals(lightType))
			{
				_this = lightType;
			}

			return _this;
		}
	}
}
