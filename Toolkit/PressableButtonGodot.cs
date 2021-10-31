using Godot;

using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Util.GodotHelper;
using Microsoft.MixedReality.Toolkit.Input;

namespace Microsoft.MixedReality.Toolkit.UI
{
	public class PressableButtonGodot : PressableButton, IMixedRealityFocusHandler
	{
		[Export]
		private string text = "";

		[Export]
		private Color backPlateColor = new Color(0.086f, 0.2f, 0.5f, 1f);
		public Color BackPlateColor { get => backPlateColor; set => backPlateColor = value; }

		private float pulseDelta = -0.6f;
		private MeshInstance BackPlate;
		private MeshInstance HighlightPlate;
		private HighlightArea HighlightArea;
		private ShaderMaterial FrontPlateMaterial;
		private ShaderMaterial BackPlateMaterial;
		private ShaderMaterial HighlightPlateMaterial;
		private SimpleText TextNode;

		public PressableButtonGodot()
		{
			//set default
			StartPushDistance = 0.032f;
			MaxPushDistance = 0.008f;
			PressDistance = 0.012f;
			ReleaseDistanceDelta = 0.004f;
			movingButtonVisualsNodePath = new NodePath("FrontPlate");
			nearInteractionTouchableSurfaceNodePath = new NodePath("NearInteractionTouchable");
		}

		public override void _Ready()
		{
			BackPlate = GetNode<MeshInstance>("BackPlate");
			BackPlateMaterial = BackPlate.MaterialOverride.Duplicate(true) as ShaderMaterial;
			BackPlate.MaterialOverride = BackPlateMaterial;
			BackPlateMaterial.SetShaderParam("color", backPlateColor);

			FrontPlateMaterial = ((MeshInstance)movingButtonVisuals).MaterialOverride.Duplicate(true) as ShaderMaterial;
			((MeshInstance)movingButtonVisuals).MaterialOverride = FrontPlateMaterial;
			FrontPlateMaterial.SetShaderParam("origin", BackPlate.GlobalTransform.origin);
			FrontPlateMaterial.SetShaderParam("backward", BackPlate.GlobalTransform.basis.z);

			HighlightPlate = movingButtonVisuals.GetNode<MeshInstance>("HighlightPlate");
			HighlightPlateMaterial = (ShaderMaterial)HighlightPlate.MaterialOverride.Duplicate(true);
			HighlightPlate.MaterialOverride = HighlightPlateMaterial;

			HighlightArea = HighlightPlate.GetNode<HighlightArea>("HighlightArea");
			HighlightPlateMaterial.SetShaderParam(HighlightArea.BorderColorString, Vector3.Zero);

			TextNode = GetNode<SimpleText>("Text");
			TextNode.Contents = text;
			TextNode.Anchor = MixedRealityExtension.Core.Interfaces.TextAnchorLocation.MiddleCenter;
			TextNode.Height = 0.4f;
			TextNode.Transform = new Transform(TextNode.Transform.basis, ToLocal(HighlightPlate.GlobalTransform.origin) / 2);
			TextNode.Scale = new Vector3(0.032f, 0.032f, 0.032f);

			Connect("touch_begin", this, nameof(_on_PressableButtonGodot_touch_begin));
			Connect("touch_end", this, nameof(_on_PressableButtonGodot_touch_end));
		}

		public override void _PhysicsProcess(float delta)
		{
			if (IsTouching && IsPressing)
			{
				PulseProximityLight(delta);
				TextNode.Transform = new Transform(TextNode.Transform.basis, ToLocal(GlobalTransform.basis.z.Normalized() * 0.001f + GlobalTransform.origin));
			}
			else
			{
				RevertProximityLight();
				TextNode.Transform = new Transform(TextNode.Transform.basis, ToLocal(HighlightPlate.GlobalTransform.origin) / 2);
			}
		}

		private void PulseProximityLight(float delta)
		{
			if (pulseDelta < 0f)
			{
				pulseDelta += delta * 3;
				HighlightPlateMaterial.SetShaderParam("pulse_delta", pulseDelta);
			}
		}

		private void RevertProximityLight()
		{
			if (pulseDelta > -0.6f)
			{
				pulseDelta = -0.6f;
				HighlightPlateMaterial.SetShaderParam("pulse_delta", pulseDelta);
			}
		}

		private void _on_PressableButtonGodot_touch_begin()
		{
			FrontPlateMaterial.SetShaderParam("origin", BackPlate.GlobalTransform.origin);
			FrontPlateMaterial.SetShaderParam("backward", BackPlate.GlobalTransform.basis.z);
		}

		private void _on_PressableButtonGodot_touch_end()
		{
			RevertProximityLight();
		}

		internal void ApplyColor(ColorPatch color)
		{
			MWColor MWColor = new MWColor();
			BackPlateColor = BackPlateColor.GetPatchApplied(MWColor.ApplyPatch(color));
			BackPlateMaterial.SetShaderParam("color", backPlateColor);
		}

		public void OnFocusEnter(MixedRealityFocusEventData eventData)
		{
			HighlightPlateMaterial.SetShaderParam(HighlightArea.BorderColorString, HighlightArea.BorderColor);
		}

		public void OnFocusExit(MixedRealityFocusEventData eventData)
		{
			HighlightPlateMaterial.SetShaderParam(HighlightArea.BorderColorString, Vector3.Zero);
		}
	}
}