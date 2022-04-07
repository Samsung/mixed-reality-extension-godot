// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using Godot;

namespace Microsoft.MixedReality.Toolkit.UI
{
	public class ToolkitText : Sprite3D
	{
		private Viewport viewport;
		private Label label;

		public string Text {
			get => label.Text;
			set
			{
				label.Text = value;
				label.Hide();
				label.Show();
				viewport.Size = label.RectSize;
				var viewportTexture = viewport.GetTexture();
				viewportTexture.Flags = (uint)Texture.FlagsEnum.Filter;
				Texture = viewportTexture;
			}
		}

		public override void _Ready()
		{
			viewport = GetNode<Viewport>("Viewport");
			label = viewport.GetNode<Label>("Label");
		}
	}
}
