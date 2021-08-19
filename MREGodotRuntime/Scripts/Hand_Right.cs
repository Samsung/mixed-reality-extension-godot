using Godot;
using System;

public class Hand_Right : Spatial
{
    private Vector2 mouseDelta = new Vector2();
    private float handMoveSpeed = 0.003f;
    public override void _Ready()
    {
        
    }

    public override void _PhysicsProcess(float delta)
    {
        Translate(new Vector3(mouseDelta.y * handMoveSpeed, 0, -mouseDelta.x * handMoveSpeed));
        mouseDelta = new Vector2();
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (Input.IsActionPressed("shift"))
        {
            if (inputEvent is InputEventMouseMotion e)
            {
                mouseDelta = e.Relative;
            }
        }
    }
}
