using Godot;
using System;

public class ThumbMetacarpal : MeshInstance
{
    Area ThumbTipArea;
    Area IndexTipArea;
    RayCast RayCast;
    MeshInstance MiddleMetacarpal;
    MeshInstance ThumbProximal;
    MeshInstance IndexProximal;
    MeshInstance MiddleProximal;
    MeshInstance LittleProximal;

    Spatial target;
    Tween tween;
    Vector3 oldPosition;
    public override void _Ready()
    {
        ThumbTipArea = GetNode<Area>("ThumbProximal/ThumbDistal/ThumbTip/ThumbTipArea");
        IndexTipArea = GetNode<Area>("../IndexMetacarpal/IndexProximal/IndexIntermediate/IndexDistal/IndexTip/IndexTipArea");
        RayCast = GetNode<RayCast>("../RayCast");
        MiddleMetacarpal = GetNode<MeshInstance>("../MiddleMetacarpal/MiddleProximal");

        ThumbProximal = GetNode<MeshInstance>("../ThumbMetacarpal/ThumbProximal");
        IndexProximal = GetNode<MeshInstance>("../IndexMetacarpal/IndexProximal");
        MiddleProximal = GetNode<MeshInstance>("../MiddleMetacarpal/MiddleProximal");
        LittleProximal = GetNode<MeshInstance>("../LittleMetacarpal/LittleProximal");
        tween = GetNode<Tween>("../Tween");
        target = GetNode<Spatial>("../Target");

        ThumbTipArea.Connect("area_entered", this, nameof(OnAreaEnter));
        ThumbTipArea.Connect("area_exited", this, nameof(OnAreaExit));
    }

    public override void _Process(float delta)
    {
        Vector3 newPosition = (ThumbProximal.GlobalTransform.origin + IndexProximal.GlobalTransform.origin + LittleProximal.GlobalTransform.origin) / 3;

        if (!tween.IsActive() && newPosition.DistanceSquaredTo(oldPosition) > 0.01f)
        {
            tween.InterpolateProperty(target, "global_transform:origin", oldPosition, newPosition, 0.1f);
            tween.Start();
            oldPosition = newPosition;
        }
        RayCast.LookAt(target.GlobalTransform.origin, Vector3.Up);
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
