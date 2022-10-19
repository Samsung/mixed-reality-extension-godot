using System;
using Assets.Scripts.User;
using Godot;

namespace Assets.Scripts.Control
{
    public abstract partial class BaseController : Node3D, IController
    {
        public InputSource InputSource { get; protected set; }

        protected virtual Cursor LoadCursor(string scenePath)
        {
            var cursorScene = ResourceLoader.Load<PackedScene>(scenePath);
            var cursor = cursorScene.Instantiate<Cursor>();
            return cursor;
        }

        protected virtual void AddInputSource(Node parent, Node userNode, string cursorScene = null, string rayScene = null)
        {
            Player player = FindParent("Player") as Player;
            Cursor cursor = null;
            if (!string.IsNullOrEmpty(cursorScene))
            {
                cursor = LoadCursor(cursorScene);
            }

            InputSource = new InputSource(userNode)
            {
                Cursor = cursor,
            };
            parent.AddChild(InputSource);

            player.CursorChanged += OnBaseControllerCursorChanged;
        }

        protected virtual void OnBaseControllerCursorChanged(string cursorPath)
        {
            if (!string.IsNullOrEmpty(cursorPath))
            {
                var newCursor = LoadCursor(cursorPath);
                InputSource.Cursor = newCursor;
            }
        }
    }
}
