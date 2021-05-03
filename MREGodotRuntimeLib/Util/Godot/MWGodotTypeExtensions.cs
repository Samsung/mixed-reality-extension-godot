// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Types;
using Godot;
//using UnityGLTF;

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

		public static MWQuaternion FromGodotQuaternion(this MWQuaternion _this, Quat other)
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

		public static MWQuaternion CreateMWQuaternion(this Quat _this)
		{
			return new MWQuaternion()
			{
				W = _this.w,
				X = _this.x,
				Y = _this.y,
				Z = _this.z
			};
		}

		public static void ToLocalTransform(this MWScaledTransform _this, Spatial spatial)
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
			_this.Rotation.FromGodotQuaternion(new Quat(spatial.Rotation));
			_this.Scale.FromGodotVector3(spatial.Scale);
		}

		public static void ToAppTransform(this MWTransform _this, Spatial transform, Spatial appRoot)
		{
			if (_this.Position == null)
			{
				_this.Position = new MWVector3();
			}

			if (_this.Rotation == null)
			{
				_this.Rotation = new MWQuaternion();
			}

			_this.Position.FromGodotVector3(appRoot.ToLocal(transform.Transform.origin));
			//FIXME
			//_this.Rotation.FromGodotQuaternion(Quaternion.Inverse(appRoot.rotation) * transform.rotation);
		}

		public static MWVector3 ToLocalMWVector3(this MWVector3 _this, Vector3 point, Spatial objectRoot)
		{
			_this.FromGodotVector3(objectRoot.ToGlobal(point));
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

		public static Quat ToQuaternion(this MWQuaternion _this)
		{
			return new Quat()
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
/*FIXME
		public static GLTFSceneImporter.ColliderType ToGLTFColliderType(this ColliderType _this)
		{
			switch (_this)
			{
				case ColliderType.Mesh:
					return GLTFSceneImporter.ColliderType.MeshConvex;
				case ColliderType.Sphere:
					MREAPI.Logger.LogWarning("Sphere colliders are not supported in UnityGLTF yet.  Downgrading to a box collider.");
					goto case ColliderType.Box;
				case ColliderType.Box:
					return GLTFSceneImporter.ColliderType.Box;
				default:
					return GLTFSceneImporter.ColliderType.None;
			}
		}
*/
	}
}
