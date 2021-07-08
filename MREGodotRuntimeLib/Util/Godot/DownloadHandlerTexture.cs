using System;
using System.IO;
using Godot;

namespace MixedRealityExtension.Util.GodotHelper
{
    internal class DownloadHandlerTexture : DownloadHandler
    {
        public Texture Texture { get; set; }
        private Uri uri;
        public DownloadHandlerTexture(Uri uri)
        {
            this.uri = uri;
        }

        public void ParseData(MemoryStream stream)
        {
            var img = new Image();

            if (System.IO.Path.GetExtension(uri.AbsolutePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                img.LoadPngFromBuffer(stream.ToArray());
            }
            else if (System.IO.Path.GetExtension(uri.AbsolutePath).Equals(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                img.LoadJpgFromBuffer(stream.ToArray());
            }
            else if (System.IO.Path.GetExtension(uri.AbsolutePath).Equals(".bmp", StringComparison.OrdinalIgnoreCase))
            {
                img.LoadBmpFromBuffer(stream.ToArray());
            }
            else if (System.IO.Path.GetExtension(uri.AbsolutePath).Equals(".tga", StringComparison.OrdinalIgnoreCase))
            {
                img.LoadTgaFromBuffer(stream.ToArray());
            }
            else if (System.IO.Path.GetExtension(uri.AbsolutePath).Equals(".webp", StringComparison.OrdinalIgnoreCase))
            {
                img.LoadWebpFromBuffer(stream.ToArray());
            }
            img.FlipY();

            var imageTexture = new ImageTexture();
            imageTexture.CreateFromImage(img);
            Texture = imageTexture;
        }
    }
}