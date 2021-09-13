using Godot;
using System;

public class IndexTip : MeshInstance
{
    bool pressed = false;
    AnimationPlayer animationPlayer;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public override void _Input(InputEvent ev)
    {
        if (Input.IsActionJustPressed("hand_touch") && !pressed)
        {
            animationPlayer.Play("touch");
            pressed = true;
        }
        else if (Input.IsActionJustReleased("hand_touch") && pressed)
        {
            pressed = false;
            animationPlayer.PlayBackwards("touch");
        }
    }
//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//
//  }
}
