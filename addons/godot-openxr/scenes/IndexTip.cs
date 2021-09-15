using Godot;
using System;

public class IndexTip : MeshInstance
{
    bool pressed = false;
    AnimationPlayer animationPlayer;
    RayCast rayCast;
    OmniLight ProximityLight;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        rayCast = GetNode<RayCast>("RayCast");
        ProximityLight = GetNode<OmniLight>("ProximityLight");
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
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (rayCast.IsColliding())
        {
            if (ProximityLight.Visible == false) ProximityLight.Visible = true;
            GD.Print(rayCast.GetCollisionPoint().DistanceSquaredTo(rayCast.GlobalTransform.origin));
            
            //ProximityLight.OmniRange = 0.
            ProximityLight.GlobalTransform = new Transform(ProximityLight.GlobalTransform.basis, rayCast.GetCollisionPoint() + rayCast.GetCollisionNormal().Normalized() * 0.01f);
        }
        else
        {
            GD.Print(pressed);
            ProximityLight.Visible = pressed;
        }
    }
}
