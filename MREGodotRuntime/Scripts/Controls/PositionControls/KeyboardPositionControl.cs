using Godot;

namespace Assets.Scripts.Control
{
    internal partial class KeyboardPositionControl : Node3D, IPositionControl
    {
        public float Speed { get; set; } = 1f;
        private Camera3D mainCamera;

        public KeyboardPositionControl(Camera3D camera)
        {
            mainCamera = camera;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (Input.IsKeyPressed(Godot.Key.Space))
                XRServer.CenterOnHmd(XRServer.RotationMode.ResetButKeepTilt, true);

            if (Input.IsActionPressed("move_right"))
            {
                mainCamera.Position += mainCamera.Transform.basis.x * (float)delta * Speed;
            }
            else if (Input.IsActionPressed("move_left"))
            {
                mainCamera.Position -= mainCamera.Transform.basis.x * (float)delta * Speed;
            }

            if (Input.IsActionPressed("move_back"))
            {
                mainCamera.Position += mainCamera.Transform.basis.z * (float)delta * Speed;
            }
            else if (Input.IsActionPressed("move_forward"))
            {
                mainCamera.Position -= mainCamera.Transform.basis.z * (float)delta * Speed;
            }
        }
    }
}