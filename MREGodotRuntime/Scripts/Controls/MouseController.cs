using Assets.Scripts.Tools;
using Assets.Scripts.User;
using Godot;

namespace Assets.Scripts.Control
{
    internal class MouseController : BaseController
    {
        private Camera mainCamera;

        public MouseController(Camera camera)
        {
            mainCamera = camera;
        }

        public override void _Ready()
        {
            var player = GetParent<Player>();
            AddInputSource(mainCamera, mainCamera, player.CursorScenePath, player.RayScenePath);
            InputSource.RayCastDistance = 10000f;
        }

        public override void _Process(float delta)
        {
            if (InputSource.CurrentTool is TargetTool targetTool)
            {
                InputSource.Cursor.Visible = InputSource.Ray.Visible = (targetTool.Target != null);
            }
        }

        public override void _Input(InputEvent inputEvent)
        {
            if (inputEvent is InputEventMouseMotion mouseMotion)
            {
                var inputSourcePosition = mainCamera.ProjectPosition(mouseMotion.Position, mainCamera.Near + 0.3f);
                InputSource.RayCastDirection = mainCamera.ProjectRayNormal(mouseMotion.Position);
                InputSource.RayCastBegin = mainCamera.ProjectRayOrigin(mouseMotion.Position);
                InputSource.GlobalTransform = new Transform(InputSource.GlobalTransform.basis, inputSourcePosition);
            }
            else if (inputEvent is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == 1)
            {
                InputSource.IsPinching = mouseButton.Pressed;
            }
        }
    }
}
