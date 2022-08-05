using Godot;

namespace Assets.Scripts.Control
{
    internal partial class KeyboardPositionControl : Spatial, IPositionControl
    {
        public float Speed { get; set; } = 1f;
        private Camera mainCamera;

        public KeyboardPositionControl(Camera camera)
        {
            mainCamera = camera;
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Input.IsKeyPressed((int)Godot.KeyList.Space))
                ARVRServer.CenterOnHmd(ARVRServer.RotationMode.ResetButKeepTilt, true);

            if (Input.IsActionPressed("move_right"))
            {
                mainCamera.Translation += mainCamera.Transform.basis.x * delta * Speed;
            }
            else if (Input.IsActionPressed("move_left"))
            {
                mainCamera.Translation -= mainCamera.Transform.basis.x * delta * Speed;
            }

            if (Input.IsActionPressed("move_back"))
            {
                mainCamera.Translation += mainCamera.Transform.basis.z * delta * Speed;
            }
            else if (Input.IsActionPressed("move_forward"))
            {
                mainCamera.Translation -= mainCamera.Transform.basis.z * delta * Speed;
            }
        }
    }
}