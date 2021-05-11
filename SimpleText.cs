// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using System.Collections.Generic;
using Godot;

public class SimpleText : IText
{
	private readonly MeshInstance textMesh;
	private readonly Label label;
	private readonly DynamicFont dynamicFont;
	private readonly Viewport textViewport;

	private static readonly float ViewportHegiht = 444;

	/// <inheritdoc />
	public bool Enabled
	{
		get { return textMesh.Visible; }
		private set { textMesh.Visible = value; }
	}

	/// <inheritdoc />
	public string Contents
	{
		get { return label.Text; }
		private set
		{
			label.Text = value;
			resizeContainer();
		}
	}

	/// <inheritdoc />
	public float Height
	{
		get { return dynamicFont.Size / 200; }
		private set
		{
			dynamicFont.Size = (int)(value * 200);
			resizeContainer();
		}
	}

	// this field is unused with SimpleText
	/// <inheritdoc />
	public float PixelsPerLine { get; private set; }

	private static readonly Dictionary<TextAnchorLocation, Vector2> Pivot = new Dictionary<TextAnchorLocation, Vector2>
	{
		{ TextAnchorLocation.TopLeft, new Vector2(0, 1)},
		{ TextAnchorLocation.TopCenter, new Vector2(0.5f, 1) },
		{ TextAnchorLocation.TopRight, new Vector2(1, 1) },
		{ TextAnchorLocation.MiddleLeft, new Vector2(0, 0.5f) },
		{ TextAnchorLocation.MiddleCenter, new Vector2(0.5f, 0.5f) },
		{ TextAnchorLocation.MiddleRight, new Vector2(1, 0.5f) },
		{ TextAnchorLocation.BottomLeft, new Vector2(0, 0) },
		{ TextAnchorLocation.BottomCenter, new Vector2(0.5f, 0) },
		{ TextAnchorLocation.BottomRight, new Vector2(1, 0) }
	};

	//FIXME
	/// <inheritdoc />
	public TextAnchorLocation Anchor
	{
		get;
		private set;
	}

	//FIXME
	/// <inheritdoc />
	public TextJustify Justify
	{
		get;
		private set;
	}

	//FIXME
	/// <inheritdoc />
	public FontFamily Font
	{
		get;
		private set;
	}

	/// <inheritdoc />
	public MWColor Color
	{
		get {
			var color = label.GetColor("font_color");
			return new MWColor(
				color.r,
				color.g,
				color.b,
				color.a);
		}
		private set {
			label.AddColorOverride("font_color", new Color(value.R, value.G, value.B, value.A));
		}
	}

	public SimpleText(IActor actor)
	{
		textMesh = new MeshInstance()
		{
			Scale = new Vector3(1, 1, 0.01f),
			Name = "FontMesh"
		};

		actor.Node3D.AddChild(textMesh);

		textViewport= new Viewport() {
			Size = new Vector2(500, 200),
			TransparentBg = true,
			RenderTargetVFlip = true,
		};

		label = new Label();
		dynamicFont = new DynamicFont()
		{
			FontData = ResourceLoader.Load<DynamicFontData>("res://Fonts/NanumSquareRound/NanumSquareRoundR.ttf"),
			Size = 60
		};
		label.AddFontOverride("font", dynamicFont);
		//label.Connect("resized", actor.Node3D, nameof(resizeContainer));

		textViewport.AddChild(label);
		textMesh.AddChild(textViewport);

		var mesh = new CubeMesh();
		textMesh.Mesh = mesh;

		var textMeshMaterial = new SpatialMaterial()
		{
			FlagsTransparent = true,
			ParamsCullMode = SpatialMaterial.CullMode.Disabled,
			AlbedoTexture = textViewport.GetTexture(),
		};

		textMesh.MaterialOverride = textMeshMaterial;

		// set defaults
		Enabled = true;
		Contents = "";
		PixelsPerLine = 50;
		Height = 1;

		Anchor = TextAnchorLocation.TopLeft;
		Justify = TextJustify.Left;
		Font = FontFamily.SansSerif;
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

	private void resizeContainer()
	{
		label.RectSize = new Vector2(dynamicFont.GetStringSize(label.Text).x, ViewportHegiht / 2);
		textViewport.Size = new Vector2(label.RectSize.x * 3f, ViewportHegiht);
		textMesh.Scale = new Vector3(label.RectSize.x * 0.005f,
									textMesh.Scale.y,
									textMesh.Scale.z);
	}
}
