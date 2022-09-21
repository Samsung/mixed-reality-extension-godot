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

        protected virtual User.Ray LoadRay(string scenePath, Camera3D mainCamera)
        {
            var rayScene = ResourceLoader.Load<PackedScene>(scenePath);
            var ray = rayScene.Instantiate<User.Ray>();
            ray.Camera3D = mainCamera;
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
                ray = LoadRay(rayScene, (Camera3D)userNode);
            }

            InputSource = new InputSource(userNode)
            {
                Cursor = cursor,
                Ray = ray,
            };
            parent.AddChild(InputSource);

            player.CursorChanged += OnBaseControllerCursorChanged;
            player.RayChanged += OnBaseControllerRayChanged;
        }

        protected virtual void OnBaseControllerCursorChanged(string cursorPath)
        {
            if (!string.IsNullOrEmpty(cursorPath))
            {
                var newCursor = LoadCursor(cursorPath);
                InputSource.Cursor = newCursor;
            }
        }

        protected virtual void OnBaseControllerRayChanged(string RayPath, Camera3D camera)
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
