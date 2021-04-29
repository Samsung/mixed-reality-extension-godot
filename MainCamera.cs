using Godot;
using System;

public class MainCamera : KinematicBody
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    private float speed = 1f;
    private float spin = 0.05f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
    public override void _PhysicsProcess(float delta)
    {
        var velocity = Vector3.Zero;

        if (Input.IsActionPressed("move_right"))
        {
            velocity.x += speed;
        }
        if (Input.IsActionPressed("move_left"))
        {
            velocity.x -= speed;
        }
        if (Input.IsActionPressed("move_back"))
        {
            velocity.z += speed;
        }
        if (Input.IsActionPressed("move_forward"))
        {
            velocity.z -= speed;
        }
        velocity = MoveAndSlide(velocity, Vector3.Up);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion e)
        {
            
            RotateY(-Lerp(0, spin, e.Relative.x / 10));
        }
    }

    private float Lerp(float firstFloat, float secondFloat, float by)
    {
        return firstFloat * (1 - by) + secondFloat * by;
    }
}
