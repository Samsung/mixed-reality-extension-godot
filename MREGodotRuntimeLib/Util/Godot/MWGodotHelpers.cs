// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Animation;
using Godot;

using MRECollisionDetectionMode = MixedRealityExtension.Core.Interfaces.CollisionDetectionMode;
using MRELightType = MixedRealityExtension.Core.Interfaces.LightType;
using GodotLightType = Godot.VisualServer.LightType;
//using UnityCollisionDetectionMode = UnityEngine.CollisionDetectionMode;

namespace MixedRealityExtension.Util.GodotHelper
{
	internal static class MWGodotHelpers
	{
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

		public static Vector3 GetPatchApplied(this Vector3 _this, MWVector3 vector)
		{
			_this.x = _this.x.GetPatchApplied(vector.X);
			_this.y = _this.y.GetPatchApplied(vector.Y);
			_this.z = _this.z.GetPatchApplied(vector.Z);

			return _this;
		}

		public static Quat GetPatchApplied(this Quat _this, MWQuaternion quaternion)
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
/*
		public static UnityCollisionDetectionMode GetPatchApplied(this UnityCollisionDetectionMode _this, MRECollisionDetectionMode value)
		{
			var detectionMode = (UnityCollisionDetectionMode)Enum.Parse(typeof(UnityCollisionDetectionMode), value.ToString());
			if (!_this.Equals(detectionMode))
			{
				_this = detectionMode;
			}

			return _this;
		}
*/
		public static GodotLightType GetPatchApplied(this VisualServer.LightType _this, MRELightType value)
		{
			var lightType = (GodotLightType)Enum.Parse(typeof(GodotLightType), value.ToString());
			if (!_this.Equals(lightType))
			{
				_this = lightType;
			}

			return _this;
		}

		public static float LargestComponentValue(this MWVector3 _this)
		{
			return Mathf.Max(_this.X, Mathf.Max(_this.Y, _this.Z));
		}

		public static int LargestComponentIndex(this MWVector3 _this)
		{
			var largest = _this.LargestComponentValue();
			if (largest == _this.X)
				return 0;
			else if (largest == _this.Y)
				return 1;
			else
				return 2;
		}

		public static float SecondLargestComponentValue(this MWVector3 _this)
		{
			return Mathf.Clamp(_this.Z, Mathf.Min(_this.X, _this.Y), Mathf.Max(_this.X, _this.Y));
		}

		public static int SecondLargestComponentIndex(this MWVector3 _this)
		{
			var second = _this.SecondLargestComponentValue();
			if (second == _this.X)
				return 0;
			else if (second == _this.Y)
				return 1;
			else
				return 2;
		}

		public static float SmallestComponentValue(this MWVector3 _this)
		{
			return Mathf.Min(_this.X, Mathf.Min(_this.Y, _this.Z));
		}

		public static int SmallestComponentIndex(this MWVector3 _this)
		{
			var largest = _this.SmallestComponentValue();
			if (largest == _this.X)
				return 0;
			else if (largest == _this.Y)
				return 1;
			else
				return 2;
		}
/*FIXME
		public static WrapMode ToUnityWrapMode(this MWAnimationWrapMode wrapMode)
		{
			global::Godot.AnimationPlayer
			switch (wrapMode)
			{
				case MWAnimationWrapMode.Loop:
					return WrapMode.Loop;

				case MWAnimationWrapMode.PingPong:
					return WrapMode.PingPong;

				case MWAnimationWrapMode.Once:
					return WrapMode.Once;

				default:
					return WrapMode.Once;
			}
		}

		public static MWAnimationWrapMode FromUnityWrapMode(this WrapMode wrapMode)
		{
			switch (wrapMode)
			{
				case WrapMode.Loop:
					return MWAnimationWrapMode.Loop;

				case WrapMode.PingPong:
					return MWAnimationWrapMode.PingPong;

				case WrapMode.Once:
					return MWAnimationWrapMode.Once;

				default:
					return MWAnimationWrapMode.Once;
			}
		}
*/

		public static T GetChild<T>(this Node _this) where T : class
		{
			var userChildCount = _this.GetChildCount();
			for (int i = 0; i < userChildCount; i++)
			{
				var child = _this.GetChild<T>(i);
				if (child != null)
					return child;
			}
			return null;
		}

		public static IEnumerable<T> GetChildren<T>(this Node _this) where T : class
		{
			var userChildCount = _this.GetChildCount();
			for (int i = 0; i < userChildCount; i++)
			{
				var child = _this.GetChild<T>(i);
				if (child != null)
					yield return child;
			}
		}
	}
}
