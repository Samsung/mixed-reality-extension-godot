using System;
using Assets.Scripts.User;
using Godot;

namespace Assets.Scripts.Control
{
    public abstract partial class BaseController : Spatial, IController
    {
        public InputSource InputSource { get; protected set; }

        protected virtual Cursor LoadCursor(string scenePath)
        {
            var cursorScene = ResourceLoader.Load<PackedScene>(scenePath);
            var cursor = cursorScene.Instance<Cursor>();
            return cursor;
        }

        protected virtual User.Ray LoadRay(string scenePath, Camera mainCamera)
        {
            var rayScene = ResourceLoader.Load<PackedScene>(scenePath);
            var ray = rayScene.Instance<User.Ray>();
            ray.Camera = mainCamera;
            return ray;
        }

        protected virtual void AddInputSource(Node parent, Node userNode, string cursorScene = null, string rayScene = null)
        {
            Player player = FindParent("Player") as Player;
            Cursor cursor = null;
            User.Ray ray = null;
            if (!string.IsNullOrEmpty(cursorScene))
            {
                cursor = LoadCursor(cursorScene);
            }

            if (!string.IsNullOrEmpty(rayScene))
            {
                ray = LoadRay(rayScene, (Camera)userNode);
            }

            InputSource = new InputSource(userNode)
            {
                Cursor = cursor,
                Ray = ray,
            };
            parent.AddChild(InputSource);

            player.Connect(nameof(Player.cursor_changed), this, nameof(_on_BaseController_cursor_changed));
            player.Connect(nameof(Player.ray_changed), this, nameof(_on_BaseController_ray_changed));
        }

        protected virtual void _on_BaseController_cursor_changed(string cursorPath)
        {
            if (!string.IsNullOrEmpty(cursorPath))
            {
                var newCursor = LoadCursor(cursorPath);
                InputSource.Cursor = newCursor;
            }
        }

        protected virtual void _on_BaseController_ray_changed(string RayPath, Camera camera)
        {
            if (camera == null)
            {
                throw new ArgumentException(nameof(camera));
            }

            if (!string.IsNullOrEmpty(RayPath))
            {
                var newRay = LoadRay(RayPath, camera);
                InputSource.Ray = newRay;
            }
        }
    }
}
