// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using Godot;

namespace MixedRealityExtension.PluginInterfaces
{
	public interface ITouchableSurface : ITouchableBase
	{
		/// <summary>
		/// The local center point of interaction.  This may be based on a collider position or Unity UI RectTransform.
		/// </summary>
		Vector3 LocalCenter { get; }

		/// <summary>
		/// This is the direction that a user will press on this element.
		/// </summary>
		Vector3 LocalPressDirection { get; }

		/// <summary>
		/// Bounds specify where touchable interactions can occur.  They are local bounds on the plane specified by the LocalCenter and LocalPressDirection (as a normal).
		/// </summary>
		Vector2 Bounds { get; }
	}
}