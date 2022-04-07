using Godot;

using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using Microsoft.MixedReality.Toolkit.Input;
using MixedRealityExtension.Core;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;

namespace Microsoft.MixedReality.Toolkit.UI
{
	public class PressableButtonGodot : PressableButton, IToolkit, IMixedRealityFocusHandler
	{
		[Export]
		private string text = "";

		[Export]
		public Color BackPlateColor { get; set; } = new Color(0.086f, 0.2f, 0.5f, 1f);

		public Vector3 HighlightBorderColor { get; set; } = new Vector3(0.42f, 0.48f, 0.61f);

		Node IToolkit.Parent => Parent;

		public const string HighlightBorderColorString = "border_color";

		private float pulseDelta = -0.6f;
		private MeshInstance BackPlate;
		private MeshInstance HighlightPlate;
		private ShaderMaterial BackPlateMaterial => (ShaderMaterial)BackPlate.MaterialOverride;
		private ShaderMaterial HighlightPlateMaterial => (ShaderMaterial)HighlightPlate.MaterialOverride;
		private ToolkitText textNode;
		private Vector3 initialLocalScale;
		private MWAction _touchAction = new MWAction();
		private IUser currentUser;

		public IUser CurrentUser
		{
			get => currentUser ?? (Parent as IActor).App.LocalUser;
			set => currentUser = value;
		}

		public PressableButtonGodot()
		{
			//set default
			StartPushDistance = 0.016f;
			MaxPushDistance = 0.004f;
			PressDistance = 0.006f;
			ReleaseDistanceDelta = 0.004f;
			movingButtonVisualsNodePath = new NodePath("FrontPlate");
		}

		public override void _Ready()
		{
			initialLocalScale = movingButtonVisuals.Scale;

			base._Ready();
			this.RegisterAction(_touchAction, "touch");
			((IMixedRealityFocusHandler)this).RegisterFocusEvent(this, Parent);

			BackPlate = GetNode<MeshInstance>("BackPlate");
			BackPlate.MaterialOverride = BackPlate.MaterialOverride.Duplicate(true) as ShaderMaterial;
			BackPlateMaterial.SetShaderParam("color", BackPlateColor);

			((MeshInstance)movingButtonVisuals).MaterialOverride = ((MeshInstance)movingButtonVisuals).MaterialOverride.Duplicate(true) as ShaderMaterial;

			HighlightPlate = movingButtonVisuals.GetNode<MeshInstance>("HighlightPlate");
			HighlightPlate.MaterialOverride = HighlightPlate.MaterialOverride.Duplicate(true) as ShaderMaterial;

			textNode = GetNode<ToolkitText>("ToolkitText");
			textNode.Text = text;
			textNode.Transform = new Transform(textNode.Transform.basis, ToLocal(HighlightPlate.GlobalTransform.origin) / 2);
			textNode.Scale = new Vector3(0.02f, 0.02f, 0.02f);
		}

		public override void _PhysicsProcess(float delta)
		{
			if (IsTouching && IsPressing)
			{
				PulseProximityLight(delta);
				textNode.Transform = new Transform(textNode.Transform.basis, ToLocal(GlobalTransform.basis.z.Normalized() * 0.001f + GlobalTransform.origin));
			}
			else
			{
				RevertProximityLight();
				textNode.Transform = new Transform(textNode.Transform.basis, ToLocal(HighlightPlate.GlobalTransform.origin) / 2);
			}
		}

		/// <inheritdoc />
		protected override void UpdateMovingVisualsPosition()
		{
			if (movingButtonVisuals != null)
			{
				var scale = initialLocalScale;
				scale.z *= (CurrentPushDistance / startPushDistance);

				movingButtonVisuals.Translation = GetLocalPositionAlongPushDirection((CurrentPushDistance - startPushDistance) / 2);
				movingButtonVisuals.Scale = scale;
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
			if (pulseDelta > -0.7f)
			{
				pulseDelta = -0.7f;
				HighlightPlateMaterial.SetShaderParam("pulse_delta", pulseDelta);
			}
		}

		internal void ApplyColor(ColorPatch color)
		{
			if (color == null) return;
			MWColor MWColor = new MWColor();
			BackPlateColor = BackPlateColor.GetPatchApplied(MWColor.ApplyPatch(color));
			BackPlateMaterial.SetShaderParam("color", BackPlateColor);
		}

		internal void ApplyText(string text)
		{
			if (text == null) return;
			textNode.Text = text;
		}

		public virtual void OnFocusEnter(Spatial inputSource, Node userNode, Spatial oldTarget, Spatial newTarget)
		{
			HighlightPlateMaterial.SetShaderParam(HighlightBorderColorString, HighlightBorderColor);
		}

		public virtual void OnFocusExit(Spatial inputSource, Node userNode, Spatial oldTarget, Spatial newTarget)
		{
			HighlightPlateMaterial.SetShaderParam(HighlightBorderColorString, Vector3.Zero);
		}

		public override void OnTouchStarted(Spatial inputSource, Node userNode, Vector3 point)
		{
			base.OnTouchStarted(inputSource, userNode, point);
			if (IsTouching)
			{
				OnInteractionStarted(userNode);
				_touchAction.StartAction(CurrentUser);
			}
		}

		public override void OnTouchCompleted(Spatial inputSource, Node userNode, Vector3 point)
		{
			base.OnTouchCompleted(inputSource, userNode, point);
			if (!IsTouching)
			{
				_touchAction.StopAction(CurrentUser);
				OnInteractionEnded();
			}
			RevertProximityLight();
		}

		public virtual void OnInteractionStarted(Node userNode)
		{
			CurrentUser = this.GetMREUser(userNode);
		}

		public virtual void OnInteractionEnded()
		{
			CurrentUser = null;
		}

		public virtual void ApplyPatch(ToolkitPatch toolkitPatch)
		{
			if (toolkitPatch is ButtonPatch patch)
			{
				touchableSurface.TouchableBoxShape = GetNode<CollisionShape>("TouchableArea/CollisionShape");
				ApplyText(patch.MainText);
				ApplyColor(patch.Color);
			}
		}
	}
}