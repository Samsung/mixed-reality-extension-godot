using Godot;

public partial class DefaultRightHand : DefaultHand
{
    private float handLocalOrigin;
    private Vector2 mouseDelta = new Vector2();
    private float speed = 0.0015f;

    public override void _Ready()
    {
        base._Ready();
        handLocalOrigin = Position.Z;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("hand_touch"))
        {
            if (Position.Z > handLocalOrigin - 0.05f)
                Position -= Transform.Basis.Z * 0.0048f;
        }
        else
        {
            if (Position.Z < handLocalOrigin)
                Position += Transform.Basis.Z * 0.0048f;
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
            Translate(new Vector3(mouseDelta.X * speed, -mouseDelta.Y * speed, 0));
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
