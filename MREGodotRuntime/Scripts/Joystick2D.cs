using Godot;

public class Joystick2D : TouchScreenButton
{
    private Player player;
    private Vector2 playerDirection;
    private float speed = 0.07f;
    private Vector2 joystickCenter;

    public override void _Ready()
    {
        player = GetParent<Player>();
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
            GetTree().SetInputAsHandled();
        }
    }

    public override void _Process(float delta)
    {
        var d = playerDirection * speed;
        player.Translation += player.Transform.basis.x * d.x;
        player.Translation += player.Transform.basis.z * d.y;
    }
}
