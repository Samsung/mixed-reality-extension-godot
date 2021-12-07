// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Assets;
using Newtonsoft.Json.Linq;
using System;

namespace MixedRealityExtension.Patching.Types
{
	/// <summary>
	/// Contains material asset info
	/// </summary>
	public class MaterialPatch : Patchable<ActorPatch>
	{
		public Guid Id { get; set; }

		[PatchProperty]
		public string Name { get; set; }

		private ColorPatch color;
		private ColorPatch savedColor;

		/// <summary>
		/// The main color of the material
		/// </summary>
		public ColorPatch Color {
			get => color;
			set
			{
				if (value == null && color != null)
				{
					savedColor = color;
					savedColor.Clear();
				}
				color = value;
			}
		}

		/// <summary>
		/// The ID of the main texture asset
		/// </summary>
		public Guid? MainTextureId { get; set; }

		/// <summary>
		/// Offset the texture by this amount as a fraction of the resolution
		/// </summary>
		public Vector3Patch MainTextureOffset { get; set; }

		/// <summary>
		/// Scale the texture by this amount in each axis
		/// </summary>
		public Vector3Patch MainTextureScale { get; set; }

		/// <summary>
		/// The lighting-independent color
		/// </summary>
		public ColorPatch EmissiveColor { get; set; }

		/// <summary>
		/// The ID of the main texture asset
		/// </summary>
		public Guid? EmissiveTextureId { get; set; }

		/// <summary>
		/// Offset the texture by this amount as a fraction of the resolution
		/// </summary>
		public Vector2Patch EmissiveTextureOffset { get; set; }

		/// <summary>
		/// Scale the texture by this amount in each axis
		/// </summary>
		public Vector2Patch EmissiveTextureScale { get; set; }

		/// <summary>
		/// How this material should treat the color/texture alpha channel
		/// </summary>
		public AlphaMode? AlphaMode { get; set; }

		/// <summary>
		/// If AlphaMode is TransparentCutout, this is the transparency threshold
		/// </summary>
		public float? AlphaCutoff { get; set; }

		public MaterialPatch()
		{
		}

		internal MaterialPatch(Guid id)
		{
			Id = id;
		}

		public override void WriteToPath(TargetPath path, JToken value, int depth)
		{
			if (depth == path.PathParts.Length)
			{
				// actors are not directly patchable, do nothing
			}
			else if (path.PathParts[depth] == "color")
			{
				if (Color == null)
				{
					if (savedColor == null)
					{
						savedColor = new ColorPatch();
					}
					color = savedColor;
				}
				Color.WriteToPath(path, value, depth + 1);
			}
			// else
				// an unrecognized path, do nothing
		}

		public override bool ReadFromPath(TargetPath path, ref JToken value, int depth)
		{
			if (path.PathParts[depth] == "color")
			{
				return Color?.ReadFromPath(path, ref value, depth + 1) ?? false;
			}
			return false;
		}

		public override void Clear()
		{
			Color = null;
		}

		public bool IsEmpty()
		{
			return Name == null
				&& Color == null
				&& MainTextureId == null
				&& MainTextureOffset == null
				&& MainTextureScale == null
				&& EmissiveColor == null
				&& EmissiveTextureId == null
				&& EmissiveTextureOffset == null
				&& EmissiveTextureScale == null
				&& AlphaMode == null
				&& AlphaCutoff == null;
		}

		public override void Restore(TargetPath path, int depth)
		{
			if (path.AnimatibleType != "material" || depth >= path.PathParts.Length) return;

			switch (path.PathParts[depth])
			{
				case "color":
					Color = savedColor ?? new ColorPatch();
					Color.Restore(path, depth + 1);
					break;
			}
		}

		public override void RestoreAll()
		{
			Color = savedColor ?? new ColorPatch();
			Color.RestoreAll();
		}
	}
}
