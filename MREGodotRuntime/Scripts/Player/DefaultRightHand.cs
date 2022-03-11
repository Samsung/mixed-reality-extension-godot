using Godot;

public class DefaultRightHand : Spatial
{
    private float handLocalOrigin;
    private Vector2 mouseDelta = new Vector2();
    private float speed = 0.0015f;

    public override void _Ready()
    {
        handLocalOrigin = Translation.z;
    }

    public override void _Process(float delta)
    {
        if (Input.IsActionPressed("hand_touch"))
        {
            if (Translation.z > handLocalOrigin - 0.05f)
                Translation -= Transform.basis.z * 0.0048f;
        }
        else
        {
            if (Translation.z < handLocalOrigin)
                Translation += Transform.basis.z * 0.0048f;
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
