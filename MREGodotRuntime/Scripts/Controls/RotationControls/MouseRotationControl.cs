using Godot;

namespace Assets.Scripts.Control
{
    internal partial class MouseRotationControl : Node3D, IRotationControl
    {
        private bool cameraMove = false;
        private Vector2 mouseDelta = new Vector2();
        private Camera3D mainCamera;

        public float CameraSpeed { get; set; } = 0.0015f;

        public MouseRotationControl(Camera3D camera)
        {
            mainCamera = camera;
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Input.IsActionPressed("ui_cancel"))
            {
                cameraMove = false;
            }
            if (!cameraMove) return;

            if (!Input.IsActionPressed("shift"))
            {
                var newRotation = mainCamera.Rotation;
                newRotation.x -= mouseDelta.y * CameraSpeed;
                newRotation.y -= mouseDelta.x * CameraSpeed;
                mainCamera.Rotation = newRotation;
            }
            mouseDelta = Vector2.Zero;
        }

        public override void _Input(InputEvent inputEvent)
        {
            if (inputEvent is InputEventMouseMotion e)
            {
                mouseDelta = e.Relative;
            }
            else if (inputEvent is InputEventMouseButton mouseButton)
            {
                if (!cameraMove && mouseButton.Pressed)
                    cameraMove = true;
            }
        }
    }
}
