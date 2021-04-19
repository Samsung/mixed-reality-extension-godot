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
	public LaunchType LaunchType;

	public MREComponent MREComponent;

	public bool StopAppOnExit = true;

	private bool _running = false;

	// Use this for initialization
	public override void _Ready()
	{
		LaunchType = LaunchType.OnStart;
		MREComponent = new MREComponent();
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
		if ((inputEvent as InputEventMouseButton).IsPressed())
		{
			if (LaunchType == LaunchType.MouseButtonDown && MREComponent != null)
			{
				StartApp();
			}
		}
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
