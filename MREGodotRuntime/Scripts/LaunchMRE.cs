using Godot;
using System;
using MixedRealityExtension.Util.GodotHelper;

public enum LaunchType
{
	MouseButtonDown,
	TriggerVolume,
	OnStart
}

[Tool]
public class LaunchMRE : Spatial
{
	private Area CollisionArea;

	private LaunchType launchType = LaunchType.OnStart;
	[Export]
	public LaunchType LaunchType
	{
		get => launchType;
		set
		{
			if (launchType == value)
				return;
			launchType = value;
			UpdateEditorSceneWithLaunchType();
		}
	}

	void UpdateEditorSceneWithLaunchType()
	{
		if (!IsInsideTree())
			return;
		switch (LaunchType)
		{
			case LaunchType.MouseButtonDown:
			case LaunchType.TriggerVolume:
				if (CollisionArea == null)
				{
					CollisionArea = this.GetChild<Area>() ?? new Area();
					AddChild(CollisionArea);
					CollisionArea.Connect("input_event", this, nameof(_InputEvent));
					CollisionArea.Owner = GetTree().EditedSceneRoot;

					var CollisionShape = CollisionArea.GetChild<CollisionShape>() ?? new CollisionShape();
					CollisionArea.AddChild(CollisionShape);
					CollisionShape.Owner = GetTree().EditedSceneRoot;


				}
				break;
			case LaunchType.OnStart:
				if (CollisionArea != null)
				{
					CollisionArea.QueueFree();
					CollisionArea = null;
				}
				break;
		}
	}

	public MREComponent MREComponent;

	[Export]
	public bool StopAppOnExit = true;

	private bool _running = false;

	//MREComponent Properties
	[Export]
	public string MREURL;

	[Export]
	public string SessionID;

	[Export]
	public string AppID;

	[Export]
	public string EphemeralAppID;

	[Export]
	public bool AutoStart = false;

	[Export]
	public bool AutoJoin = true;

	[Export]
	public NodePath UserNode;

	// Use this for initialization
	public override void _Ready()
	{
		UpdateEditorSceneWithLaunchType();
		MREComponent = new MREComponent();
		MREComponent.Name = "MREComponent";
		MREComponent.MREURL = MREURL;
		MREComponent.SessionID = SessionID;
		MREComponent.AppID = AppID;
		MREComponent.EphemeralAppID = EphemeralAppID;
		MREComponent.AutoStart = AutoStart;
		MREComponent.AutoJoin = AutoJoin;
		MREComponent.GrantedPermissions = (MixedRealityExtension.Core.Permissions)(-1);
		MREComponent.UserProperties = new MREComponent.UserProperty[0];
		MREComponent.UserNode = GetNode(UserNode);
		AddChild(MREComponent);
	}

	// Update is called once per frame
	public override void _Process(float delta)
	{
		if (!_running && LaunchType == LaunchType.OnStart)
		{
			StartApp();
		}
	}

	public void _InputEvent(Godot.Object camera, InputEvent inputEvent, Vector3 clickPosition, Vector3 clickNormal, int shapeIdx)
	{
		if ((inputEvent is InputEventMouseButton e) && e.IsPressed())
		{
			if (LaunchType == LaunchType.MouseButtonDown && MREComponent != null)
			{
				if (CollisionArea != null)
				{
					CollisionArea.QueueFree();
					CollisionArea = null;
				}
				StartApp();
			}
		}
	}

	private void StartApp()
	{
		if (Engine.EditorHint) return;
		GD.Print("Starting MRE app.");
		MREComponent?.EnableApp();
		_running = true;
	}

	private void StopApp()
	{
		MREComponent?.DisableApp();
		_running = false;
	}
	/*
	private void OnTriggerEnter(Collider other)
	{
		if (LaunchType == LaunchType.TriggerVolume && other.gameObject.tag == "Player")
		{
			StartApp();
		}
	}
	private void OnTriggerExit(Collider other)
	{
		if (StopAppOnExit)
		{
			if (LaunchType == LaunchType.TriggerVolume && other.gameObject.tag == "Player")
			{
				StopApp();
			}
		}
	}
	*/
}
