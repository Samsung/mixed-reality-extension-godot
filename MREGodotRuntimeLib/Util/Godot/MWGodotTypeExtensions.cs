// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Types;
using Godot;

using MRERigidBodyConstraints = MixedRealityExtension.Core.Interfaces.RigidBodyConstraints;

namespace MixedRealityExtension.Util.GodotHelper
{
	internal static class MWGodotTypeExtensions
	{
		public static MWVector2 FromGodotVector2(this MWVector2 _this, Vector2 other)
		{
			_this.X = other.x;
			_this.Y = other.y;
			return _this;
		}

		public static MWVector3 FromGodotVector3(this MWVector3 _this, Vector3 other)
		{
			_this.X = other.x;
			_this.Y = other.y;
			_this.Z = other.z;
			return _this;
		}

		public static MWQuaternion FromGodotQuaternion(this MWQuaternion _this, Quaternion other)
		{
			_this.W = other.w;
			_this.X = other.x;
			_this.Y = other.y;
			_this.Z = other.z;
			return _this;
		}

		public static MWColor FromGodotColor(this MWColor _this, Color other)
		{
			_this.R = other.r;
			_this.G = other.g;
			_this.B = other.b;
			_this.A = other.a;
			return _this;
		}

		public static MWVector2 CreateMWVector2(this Vector2 _this)
		{
			return new MWVector2()
			{
				X = _this.x,
				Y = _this.y
			};
		}

		public static MWVector3 CreateMWVector3(this Vector3 _this)
		{
			return new MWVector3()
			{
				X = _this.x,
				Y = _this.y,
				Z = _this.z
			};
		}

		public static MWQuaternion CreateMWQuaternion(this Quaternion _this)
		{
			return new MWQuaternion()
			{
				W = _this.w,
				X = _this.x,
				Y = _this.y,
				Z = _this.z
			};
		}

		public static void ToLocalTransform(this MWScaledTransform _this, Node3D spatial)
		{
			if (_this.Position == null)
			{
				_this.Position = new MWVector3();
			}

			if (_this.Rotation == null)
			{
				_this.Rotation = new MWQuaternion();
			}

			if (_this.Scale == null)
			{
				_this.Scale = new MWVector3();
			}

			_this.Position.FromGodotVector3(spatial.Transform.origin);
			_this.Position.Z *= -1;
			_this.Rotation.FromGodotQuaternion(spatial.Transform.basis.RotationQuat());
			_this.Rotation.X *= -1;
			_this.Rotation.Y *= -1;
			_this.Scale.FromGodotVector3(spatial.Scale);
		}

		public static void ToAppTransform(this MWTransform _this, Node3D transform, Node3D appRoot)
		{
			if (_this.Position == null)
			{
				_this.Position = new MWVector3();
			}

			if (_this.Rotation == null)
			{
				_this.Rotation = new MWQuaternion();
			}

			var globalTransform = appRoot.GlobalTransform.AffineInverse() * transform.GlobalTransform;
			var globalOrigin = globalTransform.origin;
			globalOrigin.z *= -1;
			var globalRotation = globalTransform.basis.RotationQuat();
			globalRotation.x *= -1;
			globalRotation.y *= -1;

			_this.Position.FromGodotVector3(globalOrigin);
			_this.Rotation.FromGodotQuaternion(globalRotation);
		}

		public static MWVector3 ToLocalMWVector3(this MWVector3 _this, Vector3 point, Node3D objectRoot)
		{
			_this.FromGodotVector3(objectRoot.ToLocal(point));
			_this.Z = -_this.Z;
			return _this;
		}

		public static Vector2 ToVector2(this MWVector2 _this)
		{
			return new Vector2()
			{
				x = _this.X,
				y = _this.Y
			};
		}

		public static Vector3 ToVector3(this MWVector3 _this)
		{
			return new Vector3()
			{
				x = _this.X,
				y = _this.Y,
				z = _this.Z
			};
		}

		public static Quaternion ToQuaternion(this MWQuaternion _this)
		{
			return new Quaternion()
			{
				w = _this.W,
				x = _this.X,
				y = _this.Y,
				z = _this.Z
			};
		}

		public static Color ToColor(this MWColor _this)
		{
			return new Color()
			{
				r = _this.R,
				g = _this.G,
				b = _this.B,
				a = _this.A
			};
		}

		public static MRERigidBodyConstraints GetMRERigidBodyConstraints(this Godot.RigidDynamicBody3D rigidBody)
		{
			MRERigidBodyConstraints constraints = 0;
			if (rigidBody.AxisLockLinearX)
				constraints |= MRERigidBodyConstraints.FreezePositionX;
			if (rigidBody.AxisLockLinearY)
				constraints |= MRERigidBodyConstraints.FreezePositionY;
			if (rigidBody.AxisLockLinearZ)
				constraints |= MRERigidBodyConstraints.FreezePositionZ;
			if (rigidBody.AxisLockAngularX)
				constraints |= MRERigidBodyConstraints.FreezeRotationX;
			if (rigidBody.AxisLockAngularY)
				constraints |= MRERigidBodyConstraints.FreezeRotationY;
			if (rigidBody.AxisLockAngularZ)
				constraints |= MRERigidBodyConstraints.FreezeRotationZ;
			return constraints;
		}
	}
}
