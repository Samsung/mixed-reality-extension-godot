// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using System.Collections.Generic;
using Godot;

public class SimpleText : IText
{
	private readonly MeshInstance textMeshInstance;
	private readonly QuadMesh textMesh;
	private readonly RichTextLabel label;
	private readonly DynamicFont dynamicFont;
	private readonly Viewport textViewport;

	private string plainContents;
	private float height;
	private TextAnchorLocation anchor;
	private TextJustify justify;

	/// <inheritdoc />
	public bool Enabled
	{
		get { return textMeshInstance.Visible; }
		private set { textMeshInstance.Visible = value; }
	}

	/// <inheritdoc />
	public string Contents
	{
		get { return plainContents; }
		private set
		{
			if (label.Text == value)
				return;

			label.Text = value;
			plainContents = value;
			resizeContainer();
		}
	}

	/// <inheritdoc />
	public float Height
	{
		get { return height; }
		private set
		{
			if (Mathf.IsEqualApprox(height, value))
				return;

			height = value;
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
			if (anchor == value)
				return;

			anchor = value;
			resizeContainer();
		}
	}

	/// <inheritdoc />
	public TextJustify Justify
	{
		get => justify;
		private set
		{
			switch (value)
			{
				case TextJustify.Left:
					label.BbcodeEnabled = false;
					label.Text = plainContents;
					break;
				case TextJustify.Center:
					label.BbcodeText = $"[center]{plainContents}[/center]";
					label.BbcodeEnabled = true;

					break;
				case TextJustify.Right:
					label.BbcodeText = $"[right]{plainContents}[/right]";
					label.BbcodeEnabled = true;
					break;
			}
		}
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
			var color = label.GetColor("default_color");
			return new MWColor(
				color.r,
				color.g,
				color.b,
				color.a);
		}
		private set {
			label.AddColorOverride("default_color", new Color(value.R, value.G, value.B, value.A));
		}
	}

	public SimpleText(IActor actor)
	{
		textMesh = new QuadMesh();
		textMeshInstance = new MeshInstance()
		{
			Name = "FontMesh",
			Mesh = textMesh
		};

		actor.Node3D.AddChild(textMeshInstance);

		textViewport = new Viewport() {
			Size = new Vector2(500, 500),
			TransparentBg = true,
			RenderTargetVFlip = true,
		};

		label = new RichTextLabel();
		label.ScrollActive = false;
		dynamicFont = new DynamicFont()
		{
			FontData = ResourceLoader.Load<DynamicFontData>("MREGodotRuntime/Scripts/Fonts/NanumSquareRound/NanumSquareRoundR.ttf"),
			Size = 400
		};
		label.AddFontOverride("normal_font", dynamicFont);
		//label.Connect("resized", actor.Node3D, nameof(resizeContainer));

		textViewport.AddChild(label);
		textMeshInstance.AddChild(textViewport);

		var viewportTexture = textViewport.GetTexture();
		viewportTexture.Flags |= (uint)Texture.FlagsEnum.Filter;

		var textMeshMaterial = new SpatialMaterial()
		{
			FlagsTransparent = true,
			ParamsCullMode = SpatialMaterial.CullMode.Disabled,
			AlbedoTexture = viewportTexture,
		};
		

		textMeshInstance.Mesh.SurfaceSetMaterial(0, textMeshMaterial);

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
		float width = 0f;
		var lines = label.Text.Split('\n');
		foreach (var line in lines)
		{
			var stringSize = dynamicFont.GetStringSize(line).x;
			if (width < stringSize)
				width = stringSize;
		}

		label.RectSize = new Vector2(width, lines.Length * dynamicFont.GetHeight());
		textViewport.Size = label.RectSize;
		textMesh.Size = new Vector2(label.RectSize.x / 428.84f, lines.Length) * Height;

		textMeshInstance.Translation = Pivot[Anchor] * textMeshInstance.Scale * new Vector3(textMesh.Size.x, textMesh.Size.y, 0) / 2;
	}
}
