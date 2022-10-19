using Godot;

public partial class DefaultHand : Node3D
{
	public void SetWrist(Node target)
	{
		var wristRemote = FindChild("Wrist_Remote") as RemoteTransform3D;
		wristRemote.RemotePath = wristRemote.GetPathTo(target);
	}

	public void SetThumbTip(Node target)
	{
		var thumbTipRemote = FindChild("Thumb_Tip_Remote") as RemoteTransform3D;
		thumbTipRemote.RemotePath = thumbTipRemote.GetPathTo(target);
	}

	public void SetIndexTip(Node target)
	{
		var indexTipRemote = FindChild("Index_Tip_Remote") as RemoteTransform3D;
		indexTipRemote.RemotePath = indexTipRemote.GetPathTo(target);
	}

	public void SetMiddleTip(Node target)
	{
		var middleTipRemote = FindChild("Middle_Tip_Remote") as RemoteTransform3D;
		middleTipRemote.RemotePath = middleTipRemote.GetPathTo(target);
	}
}
