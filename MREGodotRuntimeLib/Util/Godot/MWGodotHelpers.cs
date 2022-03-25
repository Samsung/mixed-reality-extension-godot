// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Animation;
using Godot;

namespace MixedRealityExtension.Util.GodotHelper
{
	public static class MWGodotHelpers
	{
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

		public static bool IsInterpolationLoopWrap(this MWAnimationWrapMode wrapMode)
		{
			switch (wrapMode)
			{
				case MWAnimationWrapMode.Loop:
					return true;

				case MWAnimationWrapMode.PingPong:
					GD.PushError("WrapMode.PingPong is not supported.");
					return true;

				case MWAnimationWrapMode.Once:
					return false;

				default:
					return true;
			}
		}

		public static T GetChild<T>(this Node _this) where T : class
		{
			var userChildCount = _this.GetChildCount();
			for (int i = 0; i < userChildCount; i++)
			{
				var child = _this.GetChild(i) as T;
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
				var child = _this.GetChild(i) as T;
				if (child != null)
					yield return child;
			}
		}

		public static IEnumerable<T> GetChildrenAll<T>(this Node _this) where T : class
		{
			if (_this is T t)
			{
				yield return t;
			}
			foreach (Node child in _this.GetChildren())
			{
				foreach (var node in GetChildrenAll<T>(child))
				{
					yield return node;
				}
			}
		}

		public static T AddNode<T>(this Node _this, T node) where T : Node
		{
			_this.AddChild(node);
			return node;
		}
	}
}
