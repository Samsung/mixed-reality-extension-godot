// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using System.Collections.Generic;
using Godot;

public partial class SimpleText : Label3D, IText
{
	private float height;
	private TextAnchorLocation anchor;
	private TextJustify? justify;
	private bool PendingUpdate;
	private Callable callable;
	private static readonly Shader TextShader = ResourceLoader.Load<Shader>("res://MREGodotRuntime/Shaders/MRESimpleTextShader.gdshader");
	private static readonly Font TextFont = ResourceLoader.Load<Font>("MREGodotRuntime/Scripts/Fonts/NanumSquareRound/NanumSquareRoundR.ttf");

	/// <inheritdoc />
	public bool Enabled
	{
		get { return Visible; }
		private set { Visible = value; }
	}

	/// <inheritdoc />
	public string Contents
	{
		get => Text;
		set => Text = value;
	}

	/// <inheritdoc />
	public float Height
	{
		get { return height; }
		set
		{
			if (Mathf.IsEqualApprox(height, value))
				return;

			height = value;
			Scale = Vector3.One * height;
		}
	}

	// this field is unused with SimpleText
	/// <inheritdoc />
	public float PixelsPerLine { get; private set; }

	private static readonly Dictionary<TextAnchorLocation, (VerticalAlignment, HorizontalAlignment)> Pivot = new Dictionary<TextAnchorLocation, (VerticalAlignment, HorizontalAlignment)>
	{
		{ TextAnchorLocation.TopLeft, (VerticalAlignment.Top, HorizontalAlignment.Left) },
		{ TextAnchorLocation.TopCenter, (VerticalAlignment.Top, HorizontalAlignment.Center) },
		{ TextAnchorLocation.TopRight, (VerticalAlignment.Top, HorizontalAlignment.Right) },
		{ TextAnchorLocation.MiddleLeft, (VerticalAlignment.Center, HorizontalAlignment.Left) },
		{ TextAnchorLocation.MiddleCenter, (VerticalAlignment.Center, HorizontalAlignment.Center) },
		{ TextAnchorLocation.MiddleRight, (VerticalAlignment.Center, HorizontalAlignment.Right) },
		{ TextAnchorLocation.BottomLeft, (VerticalAlignment.Bottom, HorizontalAlignment.Left) },
		{ TextAnchorLocation.BottomCenter, (VerticalAlignment.Bottom, HorizontalAlignment.Center) },
		{ TextAnchorLocation.BottomRight, (VerticalAlignment.Bottom, HorizontalAlignment.Right) }
	};

	/// <inheritdoc />
	public TextAnchorLocation Anchor
	{
		get => anchor;
		set
		{
			if (anchor == value)
				return;

			anchor = value;
			VerticalAlignment = Pivot[anchor].Item1;
			HorizontalAlignment = Pivot[anchor].Item2;
		}
	}

	/// <inheritdoc />
	public TextJustify Justify
	{
		get => justify ?? TextJustify.Center;
		private set
		{
			justify = value;
		}
	}

	//FIXME
	/// <inheritdoc />
	public new FontFamily Font
	{
		get;
		private set;
	}

	/// <inheritdoc />
	public MWColor Color
	{
		get {
			var color = Modulate;
			return new MWColor(
				color.r,
				color.g,
				color.b,
				color.a);
		}
		private set {
			Modulate = new Color(
				value.R,
				value.G,
				value.B,
				value.A
			);
			UpdateDeferred();
		}
	}

	public override void _Ready()
	{
		callable = new Callable(this, nameof(OnSimpleTextProcessFrame));
		((Label3D)this).Font = TextFont;
		PixelSize = 0.004f;
		FontSize = 160;
		TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
		Name = "SimpleText";

		UpdateDeferred();
	}

	public SimpleText()
	{
		// set defaults
		Enabled = true;
		Contents = "";
		PixelsPerLine = 50;
		Height = 1;

		Justify = TextJustify.Left;
		Font = FontFamily.SansSerif;
	}

	public SimpleText(Node3D parent) : this()
	{
		parent?.CallDeferred("add_child", this);
	}

	public void ApplyPatch(TextPatch patch)
	{
		Enabled = Enabled.ApplyPatch(patch.Enabled);
		Contents = Contents.ApplyPatch(patch.Contents);
		PixelsPerLine = PixelsPerLine.ApplyPatch(patch.PixelsPerLine);
		Height = Height.ApplyPatch(patch.Height);
		Anchor = Anchor.ApplyPatch(patch.Anchor);
		Justify = Justify.ApplyPatch(patch.Justify);
		Font = Font.ApplyPatch(patch.Font);
		Color = Color.ApplyPatch(patch.Color);
	}

	public void SynchronizeEngine(TextPatch patch)
	{
		ApplyPatch(patch);
	}

	private void UpdateDeferred()
	{
		if (PendingUpdate || !IsInsideTree()) {
			return;
		}

		PendingUpdate = true;

		GetTree().Connect("process_frame", callable);
	}

	private void OnSimpleTextProcessFrame()
	{
		GetTree().Disconnect("process_frame", callable);

		UpdateLabelTexture();
		PendingUpdate = false;
	}

	private void UpdateLabelTexture()
	{
		var surfaceCount = RenderingServer.MeshGetSurfaceCount(GetBase());
		for (int i = 0; i < surfaceCount; i++)
		{
			var labelMaterial = RenderingServer.MeshSurfaceGetMaterial(GetBase(), i);
			var labelTexture = RenderingServer.MaterialGetParam(labelMaterial, "texture_albedo");

			var material = RenderingServer.MaterialCreate();
			RenderingServer.MaterialSetShader(material, TextShader.GetRid());

			RenderingServer.MaterialSetParam(material, "texture_albedo", labelTexture);
			RenderingServer.MaterialSetParam(material, "albedo", Modulate);
			RenderingServer.MeshSurfaceSetMaterial(GetBase(), i, material);
		}
	}
}
