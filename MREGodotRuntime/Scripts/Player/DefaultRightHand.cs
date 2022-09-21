using Godot;

public partial class DefaultRightHand : Node3D
{
    private float handLocalOrigin;
    private Vector2 mouseDelta = new Vector2();
    private float speed = 0.0015f;

    public override void _Ready()
    {
        handLocalOrigin = Position.z;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("hand_touch"))
        {
            if (Position.z > handLocalOrigin - 0.05f)
                Position -= Transform.basis.z * 0.0048f;
        }
        else
        {
            if (Position.z < handLocalOrigin)
                Position += Transform.basis.z * 0.0048f;
        }

        if (Input.IsActionJustPressed("Fire1"))
        {
            var animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            animationPlayer?.Play("Pinch");
        }
        else if (Input.IsActionJustReleased("Fire1"))
        {
            var animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            animationPlayer?.PlayBackwards("Pinch");
        }

        if (Input.IsActionPressed("shift"))
        {
            Translate(new Vector3(mouseDelta.x * speed, -mouseDelta.y * speed, 0));
            mouseDelta = Vector2.Zero;
        }
    }
    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion e)
        {
            mouseDelta = e.Relative;
        }
    }
}
