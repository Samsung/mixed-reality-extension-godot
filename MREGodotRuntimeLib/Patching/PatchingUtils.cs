// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using Godot;

using MRECollisionDetectionMode = MixedRealityExtension.Core.Interfaces.CollisionDetectionMode;
using MRERigidBodyConstraints = MixedRealityExtension.Core.Interfaces.RigidBodyConstraints;
//using MRELight = MixedRealityExtension.Core.Light;
//using UnityCollisionDetectionMode = UnityEngine.CollisionDetectionMode;
//using UnityRigidBodyConstraints = UnityEngine.RigidbodyConstraints;
using UnityLight = Godot.Light;
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

		public static QuaternionPatch GeneratePatch(MWQuaternion _old, Quat _new)
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
				X = _old.X != _new.x ? (float?)_new.x : null,
				Y = _old.Y != _new.y ? (float?)_new.y : null,
				Z = _old.Z != _new.z ? (float?)_new.z : null,
				W = _old.W != _new.w ? (float?)_new.w : null
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

		public static TransformPatch GenerateAppTransformPatch(MWTransform _old, Spatial _new, Spatial appRoot)
		{
			if (_old == null && _new != null)
			{
				return new TransformPatch()
				{
					Position = GeneratePatch(null, appRoot.ToLocal(_new.GlobalTransform.origin)),
					Rotation = GeneratePatch(null, new Quat(appRoot.GlobalTransform.basis.Inverse() * _new.GlobalTransform.basis)),
				};
			}
			else if (_new == null)
			{
				return null;
			}

			TransformPatch transform = new TransformPatch()
			{
				Position = GeneratePatch(_old.Position, appRoot.ToLocal(_new.GlobalTransform.origin)),
				Rotation = GeneratePatch(_old.Rotation, new Quat(appRoot.GlobalTransform.basis.Inverse() * _new.GlobalTransform.basis)),
			};

			return transform.IsPatched() ? transform : null;
		}

		public static ScaledTransformPatch GenerateLocalTransformPatch(MWScaledTransform _old, Spatial _new)
		{
			if (_old == null && _new != null)
			{
				return new ScaledTransformPatch()
				{
					Position = GeneratePatch(null, _new.Transform.origin),
					Rotation = GeneratePatch(null, new Quat(_new.Transform.basis)),
					Scale = GeneratePatch(null, _new.Scale)
				};
			}
			else if (_new == null)
			{
				return null;
			}

			ScaledTransformPatch transform = new ScaledTransformPatch()
			{
				Position = GeneratePatch(_old.Position, _new.Transform.origin),
				Rotation = GeneratePatch(_old.Rotation, new Quat(_new.Transform.basis)),
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

		public static RigidBodyPatch GeneratePatch(MixedRealityExtension.Core.RigidBody _old, Godot.RigidBody _new,
			Spatial sceneRoot, bool addVelocities)
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
				DetectCollisions = GeneratePatch(_old.DetectCollisions, !_new.GetChild<CollisionShape>().Disabled),
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

		public static void ApplyLocalPatch(this Spatial _this, MWScaledTransform current, ScaledTransformPatch patch)
		{
			if (patch.Position != null)
			{
				_this.Transform = new Transform(_this.Transform.basis, _this.Transform.origin.GetPatchApplied(current.Position.ApplyPatch(patch.Position)));
			}

			if (patch.Rotation != null)
			{
				var Rotation = new Quat();
				Rotation = Rotation.GetPatchApplied(current.Rotation.ApplyPatch(patch.Rotation)).Normalized();
				_this.Rotation = Rotation.GetEuler();
			}

			if (patch.Scale != null)
			{
				_this.Scale = _this.Scale.GetPatchApplied(current.Scale.ApplyPatch(patch.Scale));
			}
		}

		public static void ApplyAppPatch(this Spatial _this, Spatial appRoot, MWTransform current, TransformPatch patch)
		{
			if (patch.Position != null)
			{
				var newAppPos = appRoot.ToLocal(_this.GlobalTransform.origin).GetPatchApplied(current.Position.ApplyPatch(patch.Position));
				_this.GlobalTransform = new Transform(_this.GlobalTransform.basis, appRoot.ToGlobal(newAppPos));
			}

			if (patch.Rotation != null)
			{
				var currAppRotation = appRoot.Transform.Inverse() * _this.Transform;
				var newAppRotation = currAppRotation.basis.Quat().GetPatchApplied(current.Rotation.ApplyPatch(patch.Rotation));
				_this.Transform = new Transform(newAppRotation, _this.Transform.origin);
			}
		}
	}
}
