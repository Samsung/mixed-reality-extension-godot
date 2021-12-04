// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.Patching.Types
{
	public class TouchablePatch : Patchable<TouchablePatch>
	{
		[PatchProperty]
		public TouchableType Type { get; set; }

		[PatchProperty]
		public TouchableDirection Direction { get; set; }

		[PatchProperty]
		public Vector2Patch Bounds { get; set; }
	}
}
