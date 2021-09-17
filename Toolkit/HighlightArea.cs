using Godot;
using System;

public class HighlightArea : Area
{
	[Signal]
	public delegate void focused();

	[Signal]
	public delegate void unfocused();

	ShaderMaterial HighlightPlateMaterial;

	public static Vector3 BorderColor = new Vector3(0.42f, 0.48f, 0.61f);

	public const string BorderColorString = "border_color";
}
