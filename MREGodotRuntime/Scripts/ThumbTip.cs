using Godot;
using System;

public partial class ThumbTip : MeshInstance
{
    Spatial indexTip;
    public override void _Ready()
    {
        indexTip = ((Spatial)GetNode("../../../../IndexMetacarpal/IndexProximal/IndexIntermediate/IndexDistal/IndexTip"));
    }

    public override void _Input(InputEvent ev)
    {
        if (Input.IsActionPressed("Fire2"))
        {
            GlobalTransform = indexTip.GlobalTransform;
        }
        else if (Input.IsActionJustReleased("Fire2"))
        {
            Transform = new Transform(Transform.basis, new Vector3(0, 0, -0.05f));
        }
    }
}
