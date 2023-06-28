using Assets.Scripts.Tools;
using Assets.Scripts.User;
using Godot;

namespace Assets.Scripts.Control
{
    internal partial class MouseController : BaseController
    {
        private Camera3D mainCamera;

        public MouseController(Camera3D camera)
        {
            mainCamera = camera;
        }

        public override void _Ready()
        {
            var player = GetParent<Player>();
            AddInputSource(mainCamera, player, player.CursorScenePath, player.RayScenePath);
            InputSource.RayCastDistance = 10000f;
        }

        public override void _Process(double delta)
        {
            if (InputSource.CurrentTool is TargetTool targetTool)
            {
                InputSource.Cursor.Visible = (targetTool.Target != null);
            }
        }

        public override void _Input(InputEvent inputEvent)
        {
            if (inputEvent is InputEventMouseMotion mouseMotion)
            {
                var inputSourcePosition = mainCamera.ProjectPosition(mouseMotion.Position, mainCamera.Near + 0.3f);
                InputSource.RayCastDirection = mainCamera.ProjectRayNormal(mouseMotion.Position);
                InputSource.RayCastBegin = mainCamera.ProjectRayOrigin(mouseMotion.Position);
                InputSource.GlobalTransform = new Transform3D(InputSource.GlobalTransform.Basis, inputSourcePosition);
            }
            else if (inputEvent is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
            {
                InputSource.IsPinching = mouseButton.Pressed;
            }
        }
    }
}
