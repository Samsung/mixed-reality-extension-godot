// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Types;
using Newtonsoft.Json.Linq;
using System;

namespace MixedRealityExtension.Patching.Types
{
	public class ColorPatch : Patchable<ColorPatch>, IEquatable<ColorPatch>
	{
		[PatchProperty]
		public float? R { get; set; }

		[PatchProperty]
		public float? G { get; set; }

		[PatchProperty]
		public float? B { get; set; }

		[PatchProperty]
		public float? A { get; set; }

		public ColorPatch()
		{ }

		internal ColorPatch(MWColor color)
		{
			R = color.R;
			G = color.G;
			B = color.B;
			A = color.A;
		}

		internal ColorPatch(Godot.Color color)
		{
			R = color.r;
			G = color.g;
			B = color.b;
			A = color.a;
		}

		public bool Equals(ColorPatch other)
		{
			if (other == null)
			{
				return false;
			}
			else
			{
				return
					R.Equals(other.R) &&
					G.Equals(other.G) &&
					B.Equals(other.B) &&
					A.Equals(other.A);
			}
		}

		public override void WriteToPath(TargetPath path, JToken value, int depth)
		{
			if (depth == path.PathParts.Length)
			{
				R = value.Value<float>("r");
				G = value.Value<float>("g");
				B = value.Value<float>("b");
				A = value.Value<float>("a");
			}
			else if (path.PathParts[depth] == "r")
			{
				R = value.Value<float>();
			}
			else if (path.PathParts[depth] == "g")
			{
				G = value.Value<float>();
			}
			else if (path.PathParts[depth] == "b")
			{
				B = value.Value<float>();
			}
			else if (path.PathParts[depth] == "a")
			{
				A = value.Value<float>();
			}

			// else
			// an unrecognized path, do nothing
		}

		public override bool ReadFromPath(TargetPath path, ref JToken value, int depth)
		{
			if (depth == path.PathParts.Length && R.HasValue && G.HasValue && B.HasValue && A.HasValue)
			{
				var oValue = (JObject)value;
				oValue.SetOrAdd("r", R.Value);
				oValue.SetOrAdd("g", G.Value);
				oValue.SetOrAdd("b", B.Value);
				oValue.SetOrAdd("a", A.Value);
				return true;
			}
			else if (path.PathParts[depth] == "r" && R.HasValue)
			{
				var vValue = (JValue)value;
				vValue.Value = R.Value;
				return true;
			}
			else if (path.PathParts[depth] == "g" && G.HasValue)
			{
				var vValue = (JValue)value;
				vValue.Value = G.Value;
				return true;
			}
			else if (path.PathParts[depth] == "b" && B.HasValue)
			{
				var vValue = (JValue)value;
				vValue.Value = B.Value;
				return true;
			}
			else if (path.PathParts[depth] == "a" && A.HasValue)
			{
				var vValue = (JValue)value;
				vValue.Value = A.Value;
				return true;
			}
			return false;
		}

		public override void Clear()
		{
			R = null;
			G = null;
			B = null;
			A = null;
		}
	}
}
