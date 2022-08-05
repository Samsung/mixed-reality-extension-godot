using Godot;
using System;

public partial class ThumbTip : MeshInstance3D
{
    Node3D indexTip;
    public override void _Ready()
    {
        indexTip = ((Node3D)GetNode("../../../../IndexMetacarpal/IndexProximal/IndexIntermediate/IndexDistal/IndexTip"));
    }

    public override void _Input(InputEvent ev)
    {
        if (Input.IsActionPressed("Fire2"))
        {
            GlobalTransform = indexTip.GlobalTransform;
        }
        else if (Input.IsActionJustReleased("Fire2"))
        {
            Transform = new Transform3D(Transform.basis, new Vector3(0, 0, -0.05f));
        }
    }
}
