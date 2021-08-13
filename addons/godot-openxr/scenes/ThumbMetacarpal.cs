using Godot;
using System;

public class ThumbMetacarpal : MeshInstance
{
    Area ThumbTipArea;
    Area IndexTipArea;
    RayCast RayCast;
    MeshInstance MiddleMetacarpal;
    public override void _Ready()
    {
        ThumbTipArea = GetNode<Area>("ThumbProximal/ThumbDistal/ThumbTip/ThumbTipArea");
        IndexTipArea = GetNode<Area>("../IndexMetacarpal/IndexProximal/IndexIntermediate/IndexDistal/IndexTip/IndexTipArea");
        RayCast = GetNode<RayCast>("../RayCast");
        MiddleMetacarpal = GetNode<MeshInstance>("../MiddleMetacarpal/MiddleProximal");
        ThumbTipArea.Connect("area_entered", this, nameof(OnAreaEnter));
        ThumbTipArea.Connect("area_exited", this, nameof(OnAreaExit));
    }

    public override void _Process(float delta)
    {
        RayCast.LookAt(MiddleMetacarpal.GlobalTransform.origin, Vector3.Up);
    }

    private void OnAreaEnter(Area area)
    {
        if (area == IndexTipArea)
        {
            Press();
        }
    }

    private void OnAreaExit(Area area)
    {
        if (area == IndexTipArea)
        {
            Unpress();
        }
    }

    private void Press()
    {
        Input.ActionPress("Fire1");
    }
    private void Unpress()
    {
        Input.ActionRelease("Fire1");
    }
}
