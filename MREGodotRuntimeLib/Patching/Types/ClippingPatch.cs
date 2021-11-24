// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using System;

namespace MixedRealityExtension.Patching.Types
{
	public class ClippingPatch : Patchable<ClippingPatch>
	{
		[PatchProperty]
		public Guid[] ClippingObjects { get; set; }
	}
}
