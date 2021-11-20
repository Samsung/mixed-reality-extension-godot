// Licensed under the MIT License.

using System;

namespace MixedRealityExtension.Patching.Types
{
	public class PinchSliderPatch : Patchable<PinchSliderPatch>, ToolkitPatch
	{
		public Type ToolkitType { get; set; }

		[PatchProperty]
		public Guid ThumbId { get; set; }
	}
}
