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
	private TextAnchorLocation anchor;

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

	private static readonly Dictionary<TextAnchorLocation, Vector3> Pivot = new Dictionary<TextAnchorLocation, Vector3>
	{
		{ TextAnchorLocation.TopLeft, new Vector3(1, -1, 0)},
		{ TextAnchorLocation.TopCenter, new Vector3(0, -1, 0) },
		{ TextAnchorLocation.TopRight, new Vector3(-1, -1, 0) },
		{ TextAnchorLocation.MiddleLeft, new Vector3(1, 0, 0) },
		{ TextAnchorLocation.MiddleCenter, new Vector3(0, 0, 0) },
		{ TextAnchorLocation.MiddleRight, new Vector3(-1, 0, 0) },
		{ TextAnchorLocation.BottomLeft, new Vector3(1, 1, 0) },
		{ TextAnchorLocation.BottomCenter, new Vector3(0, 1, 0) },
		{ TextAnchorLocation.BottomRight, new Vector3(-1, 1, 0) }
	};

	/// <inheritdoc />
	public TextAnchorLocation Anchor
	{
		get => anchor;
		private set
		{
			anchor = value;
			resizeContainer();
		}
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
			FontData = ResourceLoader.Load<DynamicFontData>("MREGodotRuntime/Scripts/Fonts/NanumSquareRound/NanumSquareRoundR.ttf"),
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
		label.RectSize = dynamicFont.GetStringSize(label.Text);
		textViewport.Size = new Vector2(label.RectSize.x * 3f, label.RectSize.y * 2f);
		textMesh.Scale = new Vector3(label.RectSize.x * 0.003f,
									label.RectSize.y * 0.003f,
									textMesh.Scale.z);
		textMesh.Translation = Pivot[Anchor] * textMesh.Scale;
	}
}
