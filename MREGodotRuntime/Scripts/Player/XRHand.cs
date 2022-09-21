using Godot;

public partial class XRHand : OpenXRHand
{
	[Export]
	public new int Hand {
		get => ((OpenXRHand)this).Hand;
		set => ((OpenXRHand)this).Hand = value;
	}

	public override void _Ready()
	{
		Ready();
	}

	public override void _PhysicsProcess(double delta)
	{
		PhysicsProcess();
	}
}
