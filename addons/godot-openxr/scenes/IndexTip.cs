using Godot;
using System;

public class IndexTip : MeshInstance
{
    bool pressed = false;
    AnimationPlayer animationPlayerTouch;
    RayCast rayCast;
    OmniLight ProximityLight;

    Spatial CurrentRayIntersection;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        animationPlayerTouch = GetNode<AnimationPlayer>("AnimationPlayer");
        rayCast = GetNode<RayCast>("RayCast");
        ProximityLight = GetNode<OmniLight>("ProximityLight");
    }

    public override void _Input(InputEvent ev)
    {
        if (Input.IsActionJustPressed("hand_touch") && !pressed)
        {
            animationPlayerTouch.Play("touch");
            pressed = true;
        }
        else if (Input.IsActionJustReleased("hand_touch") && pressed)
        {
            pressed = false;
            animationPlayerTouch.PlayBackwards("touch");
        }
    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (rayCast.IsColliding())
        {
            var prevRayIntersection = CurrentRayIntersection;
            CurrentRayIntersection = rayCast.GetCollider() as Spatial;
            if (ProximityLight.Visible == false) ProximityLight.Visible = true;

            if (prevRayIntersection != CurrentRayIntersection)
            {
                prevRayIntersection?.EmitSignal("unfocused");
                CurrentRayIntersection.EmitSignal("focused");
            }

            ProximityLight.GlobalTransform = new Transform(ProximityLight.GlobalTransform.basis, rayCast.GetCollisionPoint() + rayCast.GetCollisionNormal().Normalized() * 0.01f);
        }
        else
        {
            ProximityLight.Visible = pressed;
            if (!ProximityLight.Visible && CurrentRayIntersection != null)
            {
                CurrentRayIntersection.EmitSignal("unfocused");
                CurrentRayIntersection = null;
            }
        }
    }
}
