using Godot;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching.Types;

namespace Microsoft.MixedReality.Toolkit.UI
{
	public class TogglePressableButtonGodot : PressableButtonGodot
	{
		private bool isToggled = false;

		private MWAction<ActionData<bool>> _toggleChangedAction = new MWAction<ActionData<bool>>();

		[Signal]
		public delegate void toggle_changed();

		[Export]
		public bool IsToggled
		{
			get => isToggled;
			set {
				if (isToggled == value) return;
				isToggled = value;
				BackPlateToggleState.Visible = isToggled;
				EmitSignal(nameof(toggle_changed));
				_toggleChangedAction.StartAction(CurrentUser, new ActionData<bool>()
				{
					value = isToggled
				});
			}
		}

		private Spatial BackPlateToggleState;
		public override void _Ready()
		{
			base._Ready();
			BackPlateToggleState = GetNode<Spatial>("BackPlateToggleState");

			this.RegisterAction(_toggleChangedAction, "toggle_changed");
			Connect(nameof(button_pressed), this, nameof(_on_TogglePressableButtonGodot_button_pressed));
		}

		internal void ApplyIsToggled(bool? isToggled)
		{
			if (isToggled == null) return;
			IsToggled = (bool)isToggled;
		}

		private void _on_TogglePressableButtonGodot_button_pressed()
		{
			IsToggled = !IsToggled;
		}

		public override void ApplyPatch(ToolkitPatch toolkitPatch)
		{
			if (toolkitPatch is ToggleButtonPatch patch)
			{
				ApplyText(patch.MainText);
				ApplyColor(patch.Color);
				ApplyIsToggled(patch.IsToggled);
			}
		}
	}
}