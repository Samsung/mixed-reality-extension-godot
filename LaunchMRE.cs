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
	public LaunchType LaunchType;

	[Export]
	public MREComponent MREComponent;

	[Export]
	public bool StopAppOnExit = true;

	private bool _running = false;

	public LaunchMRE()
	{

	}
	// Use this for initialization
	public override void _Ready()
	{
		MREComponent = new MREComponent();
		MREComponent.Name = "MREComponent";
		MREComponent.MREURL = "ws://localhost:3901";
		MREComponent.SessionID = "testbed";
		MREComponent.AppID = "helloworld";
		MREComponent.EphemeralAppID = "helloworld-temp";
		MREComponent.AutoStart = false;
		MREComponent.AutoJoin = true;
		MREComponent.GrantedPermissions = (MixedRealityExtension.Core.Permissions)(-1);
		MREComponent.UserProperties = new MREComponent.UserProperty[0];
		MREComponent.UserNode = this.GetNode("Player");//   new Node(); // FIXME: cursor?
		AddChild(MREComponent);

		LaunchType = LaunchType.OnStart;

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
