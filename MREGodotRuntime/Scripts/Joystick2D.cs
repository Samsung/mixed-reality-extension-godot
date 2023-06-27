using Godot;

public partial class Joystick2D : TouchScreenButton
{
    private Vector2 playerDirection;
    private Vector2 joystickCenter;

    public float Speed { get; set; } = 0.07f;

    public Camera3D MainCamera { get; set; }

    public override void _Ready()
    {
        joystickCenter = Position + new Vector2(125, 125);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!IsPressed()) return;
        Vector2? touchPosition = null;
        if (inputEvent is InputEventScreenDrag screenDrag)
            touchPosition = screenDrag.Position;
        else if (inputEvent is InputEventScreenTouch screenTouch)
        {
            if (!screenTouch.Pressed)
            {
                playerDirection = Vector2.Zero;
                return;
            }

            touchPosition = screenTouch.Position;
        }

        if (touchPosition != null)
        {
            playerDirection = ((Vector2)touchPosition - joystickCenter).Normalized();
            GetTree().Root.SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        var d = playerDirection * Speed;
        MainCamera.Position += MainCamera.Transform.Basis.X * d.X;
        MainCamera.Position += MainCamera.Transform.Basis.Z * d.Y;
    }
}
