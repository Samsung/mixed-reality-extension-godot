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
				X = _old.X != _new.X ? (float?)_new.X : null,
				Y = _old.Y != _new.Y ? (float?)_new.Y : null,
				Z = _old.Z != _new.Z ? (float?)_new.Z : null
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
				X = Math.Abs(_old.X - _new.X) > 0.001f ? (float?)_new.X : null,
				Y = Math.Abs(_old.Y - _new.Y) > 0.001f ? (float?)_new.Y : null,
				Z = Math.Abs(_old.Z - _new.Z) > 0.001f ? (float?)_new.Z : null,
				W = Math.Abs(_old.W - _new.W) > 0.001f ? (float?)_new.W : null
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
			var globalOrigin = globalTransform.Origin;
			globalOrigin.Z *= -1;
			var globalRotation = globalTransform.Basis.GetRotationQuaternion();
			globalRotation.X *= -1;
			globalRotation.Y *= -1;

			TransformPatch transform = new TransformPatch()
			{
				Position = GeneratePatch(_old?.Position, globalOrigin),
				Rotation = GeneratePatch(_old?.Rotation, globalRotation),
			};

			return transform.IsPatched() ? transform : null;
		}

		public static ScaledTransformPatch GenerateLocalTransformPatch(MWScaledTransform _old, Node3D _new)
		{
			var position = _new.Transform.Origin;
			position.Z *= -1;
			var rotation = _new.Transform.Basis.GetRotationQuaternion();
			rotation.X *= -1;
			rotation.Y *= -1;


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
				R = _old.R != _new.R ? (float?)_new.R : null,
				G = _old.G != _new.G ? (float?)_new.G : null,
				B = _old.B != _new.B ? (float?)_new.B : null,
				A = _old.A != _new.A ? (float?)_new.A : null
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
			var localPosition = _this.Transform.Origin;
			var localRotation = _this.Transform.Basis.GetRotationQuaternion();
			var localScale = _this.Scale;

			if (patch.Position != null)
			{
				localPosition = localPosition.GetPatchApplied(current.Position.ApplyPatch(patch.Position));
				localPosition.Z *= -1;
			}

			if (patch.Rotation != null)
			{
				localRotation = localRotation.GetPatchApplied(current.Rotation.ApplyPatch(patch.Rotation));
				localRotation.X *= -1;
				localRotation.Y *= -1;
			}

			if (patch.Scale != null)
			{
				localScale = localScale.GetPatchApplied(current.Scale.ApplyPatch(patch.Scale));
			}

			var basis = new Basis(localRotation);
			basis = basis.Scaled(localScale / basis.Scale);
			_this.Transform = new Transform3D(basis, localPosition);
		}

		public static void ApplyAppPatch(this Node3D _this, Node3D appRoot, MWTransform current, TransformPatch patch)
		{
			var globalPosition = _this.GlobalTransform.Origin;
			var globalRotation = _this.GlobalTransform.Basis.GetRotationQuaternion();
			var globalScale = _this.GlobalTransform.Basis.Scale;

			if (patch.Position != null)
			{
				globalPosition = appRoot.ToLocal(_this.GlobalTransform.Origin).GetPatchApplied(current.Position.ApplyPatch(patch.Position));
				globalPosition.Z *= -1;
				globalPosition = appRoot.ToGlobal(globalPosition);
			}

			if (patch.Rotation != null)
			{
				var appRotation = appRoot.GlobalTransform.Basis.GetRotationQuaternion();
				var currAppRotation = appRotation.Inverse() * globalRotation;
				var newAppRotation = currAppRotation.GetPatchApplied(current.Rotation.ApplyPatch(patch.Rotation));
				newAppRotation.X *= -1;
				newAppRotation.Y *= -1;
				globalRotation = appRotation * newAppRotation;
			}

			var basis = new Basis(globalRotation);
			basis = basis.Scaled(globalScale / basis.Scale);
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
			_this.X = _this.X.GetPatchApplied(vector.X);
			_this.Y = _this.Y.GetPatchApplied(vector.Y);

			return _this;
		}

		public static Vector3 GetPatchApplied(this Vector3 _this, MWVector3 vector)
		{
			_this.X = _this.X.GetPatchApplied(vector.X);
			_this.Y = _this.Y.GetPatchApplied(vector.Y);
			_this.Z = _this.Z.GetPatchApplied(vector.Z);

			return _this;
		}

		public static Quaternion GetPatchApplied(this Quaternion _this, MWQuaternion quaternion)
		{
			_this.W = _this.W.GetPatchApplied(quaternion.W);
			_this.X = _this.X.GetPatchApplied(quaternion.X);
			_this.Y = _this.Y.GetPatchApplied(quaternion.Y);
			_this.Z = _this.Z.GetPatchApplied(quaternion.Z);

			return _this;
		}

		public static Color GetPatchApplied(this Color _this, MWColor color)
		{
			_this.R = _this.R.GetPatchApplied(color.R);
			_this.G = _this.G.GetPatchApplied(color.G);
			_this.B = _this.B.GetPatchApplied(color.B);
			_this.A = _this.A.GetPatchApplied(color.A);

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
