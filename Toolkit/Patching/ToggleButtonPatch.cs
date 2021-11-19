// Licensed under the MIT License.

using System;

namespace MixedRealityExtension.Patching.Types
{
	public class ToggleButtonPatch : Patchable<ToggleButtonPatch>, ToolkitPatch
	{
		public Type ToolkitType { get; set; }

		[PatchProperty]
		public ColorPatch Color { get; set; }

		[PatchProperty]
		public string MainText { get; set; }

		[PatchProperty]
		public bool? IsToggled { get; set; }
	}
}
