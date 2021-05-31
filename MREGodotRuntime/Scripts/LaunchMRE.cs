using Godot;
using System;

public enum LaunchType
{
	MouseButtonDown,
	TriggerVolume,
	OnStart
}

public class LaunchMRE : Spatial
{
	[Export]
	public LaunchType LaunchType = LaunchType.OnStart;

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
		AddChild(MREComponent.UserNode);
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

	public override void _Input(InputEvent inputEvent)
	{
		/*
		if ((inputEvent as InputEventMouseButton).IsPressed())
		{
			if (LaunchType == LaunchType.MouseButtonDown && MREComponent != null)
			{
				StartApp();
			}
		}
		*/
	}

	private void StartApp()
	{
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
