// Licensed under the MIT License.

using System;

namespace MixedRealityExtension.Patching.Types
{
	public class ButtonPatch : Patchable<ButtonPatch>, ToolkitPatch
	{
		public Type ToolkitType { get; set; }

		[PatchProperty]
		public ColorPatch Color { get; set; }

		[PatchProperty]
		public string MainText { get; set; }
	}
}
