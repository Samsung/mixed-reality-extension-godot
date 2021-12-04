// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using Godot;

namespace MixedRealityExtension.PluginInterfaces
{
	public interface ITouchableBase
	{
		/// <summary>
		/// Distance in front of the surface at which you will receive a touch completed event.
		/// </summary>
		/// <remarks>
		/// <para>When the touchable is active and the pointer distance becomes greater than +DebounceThreshold (i.e. in front of the surface),
		/// then the Touch Completed event is raised and the touchable object is released by the pointer.</para>
		/// </remarks>
		float DebounceThreshold { get; set; }

		float DistanceToTouchable(Vector3 samplePoint, out Vector3 normal);
	}
}