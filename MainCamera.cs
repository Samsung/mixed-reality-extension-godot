using Godot;
using System;

public class MainCamera : KinematicBody
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    private float speed = 5f;
    private float cameraSpeed = 0.5f;
    private float spin = 0.05f;
    private Camera camera;
    private Vector2 mouseDelta = new Vector2();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        camera = GetNode<Camera>("MainCamera");
    }

    public override void _PhysicsProcess(float delta)
    {
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
        velocity = MoveAndSlide(velocity);

    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion e)
        {
            mouseDelta = e.Relative;
            
            //RotateY(-Lerp(0, spin, e.Relative.x / 10));
        }
    }

    private float Lerp(float firstFloat, float secondFloat, float by)
    {
        return firstFloat * (1 - by) + secondFloat * by;
    }
}
