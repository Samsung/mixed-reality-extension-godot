
using System;

namespace MixedRealityExtension.Patching.Types
{
	public class ScrollingObjectCollectionPatch : Patchable<ScrollingObjectCollectionPatch>, ToolkitPatch
	{
		public Type ToolkitType { get; set; }
		public Guid[] ScrollContents;
		public Microsoft.MixedReality.Toolkit.UI.ScrollingObjectCollection.ScrollDirectionType? ScrollDirectionType;
		public int? CellsPerTier;
		public int? TiersPerPage;
		public float? CellWidth;
		public float? CellHeight;
		public float? CellDepth;
	}
}