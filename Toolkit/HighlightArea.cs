using Godot;
using System;

public class HighlightArea : Area
{
	[Signal]
	public delegate void focused();

	[Signal]
	public delegate void unfocused();

	ShaderMaterial HighlightPlateMaterial;

	Vector3 BorderColor = new Vector3(0.42f, 0.48f, 0.61f);

	private const string BorderColorString = "border_color";

	public override void _Ready()
	{
		HighlightPlateMaterial = GetParent<MeshInstance>().MaterialOverride as ShaderMaterial;
		HighlightPlateMaterial.SetShaderParam(BorderColorString, Vector3.Zero);
		Connect("focused", this, nameof(_on_HighlightArea_focused));
		Connect("unfocused", this, nameof(_on_HighlightArea_unfocused));
	}

	private void _on_HighlightArea_focused()
	{
		HighlightPlateMaterial.SetShaderParam(BorderColorString, BorderColor);
	}

	private void _on_HighlightArea_unfocused()
	{
		HighlightPlateMaterial.SetShaderParam(BorderColorString, Vector3.Zero);
	}
}
