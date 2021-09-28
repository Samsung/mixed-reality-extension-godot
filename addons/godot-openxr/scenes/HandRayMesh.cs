using Godot;
using System;

public class HandRayMesh : MeshInstance
{
    Spatial Player;
    public override void _Ready()
    {
        Player = GetNode<Spatial>("../../../..");
    }

    public override void _Process(float delta)
    {
        var localPosition = ToLocal(Player.GlobalTransform.origin) * Scale;
        Rotate(Transform.basis.z.Normalized(), Mathf.Atan2(localPosition.y, localPosition.x) - Mathf.Pi / 2);
    }
}
