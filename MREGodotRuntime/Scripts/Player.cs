using Godot;

public class Player : KinematicBody
{
    private float speed = 4f;
    private float cameraSpeed = 0.3f;
    private bool cameraMove = false;
    private Camera camera;
    private Vector2 mouseDelta = new Vector2();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        camera = GetNode<Camera>("MainCamera");
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!cameraMove) return;
        camera.RotationDegrees = new Vector3(camera.RotationDegrees.x - mouseDelta.y * cameraSpeed,
                                             camera.RotationDegrees.y - mouseDelta.x * cameraSpeed,
                                             camera.RotationDegrees.z);

        mouseDelta = new Vector2();
        var velocity = Vector3.Zero;

        //Transform = camera.Transform;
        if (Input.IsActionPressed("move_right"))
        {
            velocity += camera.GlobalTransform.basis.x;
        }
        if (Input.IsActionPressed("move_left"))
        {
            velocity -= camera.GlobalTransform.basis.x;
        }
        if (Input.IsActionPressed("move_back"))
        {
            velocity += camera.GlobalTransform.basis.z;
        }
        if (Input.IsActionPressed("move_forward"))
        {
            velocity -= camera.GlobalTransform.basis.z;
        }
        velocity = MoveAndSlide(velocity * speed);

    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion e)
        {
            mouseDelta = e.Relative;
        }
        else if (inputEvent is InputEventMouseButton eventMouseButton)
        {
            if (!cameraMove && eventMouseButton.Pressed)
                cameraMove = true;
        }
        else if (Input.IsActionPressed("ui_cancel"))
        {
            cameraMove = false;
        }
    }

    private float Lerp(float firstFloat, float secondFloat, float by)
    {
        return firstFloat * (1 - by) + secondFloat * by;
    }
}
