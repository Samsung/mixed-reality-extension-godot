using Godot;
using System;

public class ThumbMetacarpal : MeshInstance
{
    RayCast HandRay;
    MeshInstance Wrist;
    Spatial[] MetacarpalBones = new Spatial[10];
    Spatial Player;

    public override void _Ready()
    {
        Player = GetNode<Spatial>("../../..");
        Wrist = GetParent<MeshInstance>();
        HandRay = GetNode<RayCast>("../HandRay");

        for (int i = 0; i < 10; i += 2)
        {
            var child = Wrist.GetChild<Spatial>(i / 2);
            MetacarpalBones[i] = child;
            MetacarpalBones[i + 1] = child.GetChild<Spatial>(0);
        }
    }

    public override void _Process(float delta)
    {
        /*
        Vector3 center = Vector3.Zero;
        for (int i = 0; i < 10; i++)
        {
            center += MetacarpalBones[i].GlobalTransform.origin;
        }
        center /= 10;

        HandRay.GlobalTransform = new Transform(Player.GlobalTransform.basis, center);
        */
    }
}
