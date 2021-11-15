using Godot;

namespace Microsoft.MixedReality.Toolkit.UI
{
	public class TogglePressableButtonGodot : PressableButtonGodot
	{
		private bool isToggled = false;

		[Export]
		public bool IsToggled
		{
			get => isToggled;
			set {
				if (isToggled == value) return;
				isToggled = value;
				BackPlateToggleState.Visible = isToggled;
			}
		}

		private Spatial BackPlateToggleState;
		public override void _Ready()
		{
			base._Ready();
			BackPlateToggleState = GetNode<Spatial>("BackPlateToggleState");
			Connect("button_pressed", this, nameof(_on_TogglePressableButton_button_pressed));
		}

		internal void ApplyIsToggled(bool? isToggled)
		{
			if (isToggled == null) return;
			IsToggled = (bool)isToggled;
		}

		private void _on_TogglePressableButton_button_pressed()
		{
			IsToggled = !IsToggled;
		}
	}
}