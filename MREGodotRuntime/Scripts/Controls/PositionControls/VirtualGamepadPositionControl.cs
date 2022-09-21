using Godot;

namespace Assets.Scripts.Control
{
    public partial class VirtualGamepadPositionControl : Node3D, IPositionControl
    {
        private Joystick2D gamepad;
        private Camera3D mainCamera;

        public float Speed { get; set; } = 0.07f;

        public VirtualGamepadPositionControl(Camera3D camera)
        {
            mainCamera = camera;
        }

        private void _on_VirtualGamepadPositionControl_gamepad_changed(string gamepadPath)
        {
            if (gamepad != null)
            {
                gamepad.QueueFree();
                var newGamepad = LoadGamepad(gamepadPath);
                AddChild(newGamepad);

                gamepad = newGamepad;
            }
        }

        private Joystick2D LoadGamepad(string scenePath)
        {
            var gamepadScene = ResourceLoader.Load<PackedScene>(scenePath);
            var gamepad = gamepadScene.Instantiate<Joystick2D>();
            gamepad.MainCamera = mainCamera;
            gamepad.Speed = Speed;
            return gamepad;
        }

        public override void _Ready()
        {
            var player = GetParent<Player>();
            var gamepad = LoadGamepad(player.GamePadScenePath);
            AddChild(gamepad);

            player.Connect(nameof(Player.GamepadChangedEventHandler), new Callable(this, nameof(_on_VirtualGamepadPositionControl_gamepad_changed)));
        }
    }
}
