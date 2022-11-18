// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.GodotHelper;
using Godot;
using MRELightType = MixedRealityExtension.Core.Interfaces.LightType;
using GodotLight = Godot.Light3D;

namespace MixedRealityExtension.Core
{
	internal class Light : ILight
	{
		private readonly GodotLight _light;

		// Cached values
		private readonly MWColor _color = new MWColor();

		/// <inheritdoc />
		public bool Enabled => _light.Visible;

		/// <inheritdoc />
		public MRELightType Type {
			get {
				switch (_light)
				{
					case SpotLight3D _:
						return MRELightType.Spot;
					case OmniLight3D _:
						return MRELightType.Point;
					case DirectionalLight3D _:
						return MRELightType.Directional;
				}
				return MRELightType.Spot;
			}
		}

		/// <inheritdoc />
		public MWColor Color => _color.FromGodotColor(_light.LightColor);

		/// <inheritdoc />
		public float Range {
			get {
				switch (_light)
				{
					case SpotLight3D spotLight:
						return spotLight.SpotRange;
					case OmniLight3D omniLight:
						return omniLight.OmniRange;
					case DirectionalLight3D _:
						return float.MaxValue;
				}
				return 0;
			}
		}

		/// <inheritdoc />
		public float Intensity => _light.LightEnergy;

		/// <inheritdoc />
		public float SpotAngle {
			get {
				if (!(_light is SpotLight3D))
					return 0;
				switch (_light)
				{
					case SpotLight3D spotLight:
						return spotLight.SpotRange;
					case OmniLight3D omniLight:
						return omniLight.OmniRange;
					case DirectionalLight3D _:
						return float.MaxValue;
				}
				return 0;
			}
		}// => _light.spo * Mathf.DegToRad;

		/// <summary>
		/// Initializes a new instance of the <see cref="Light"/> class.
		/// </summary>
		/// <param name="light">The <see cref="Light"/> object to bind to.</param>
		public Light(GodotLight light)
		{
			_light = light;
		}

		/// <inheritdoc />
		public void ApplyPatch(LightPatch patch)
		{
			_light.Visible = _light.Visible.GetPatchApplied(Enabled.ApplyPatch(patch.Enabled));
			_light.LightColor = _light.LightColor.GetPatchApplied(Color.ApplyPatch(patch.Color));
			_light.LightEnergy = _light.LightEnergy.GetPatchApplied(Intensity.ApplyPatch(patch.Intensity)) * 0.5f;
			if (_light is SpotLight3D spotLight)
			{
				if (patch.SpotAngle.HasValue)
					spotLight.SpotAngle = Mathf.RadToDeg(patch.SpotAngle.Value) * 0.5f;
				spotLight.SpotRange = spotLight.SpotRange.GetPatchApplied(Range.ApplyPatch(patch.Range));
			}
			else if (_light is OmniLight3D omniLight)
			{
				omniLight.OmniRange = omniLight.OmniRange.GetPatchApplied(Range.ApplyPatch(patch.Range));
			}
		}

		/// <inheritdoc />
		public void SynchronizeEngine(LightPatch patch)
		{
			ApplyPatch(patch);
		}
	}
}
