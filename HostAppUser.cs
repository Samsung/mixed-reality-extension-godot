// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.IPC;
using MixedRealityExtension.PluginInterfaces;
using System.Collections.Generic;
using Godot;

internal class HostAppUser : IHostAppUser
{
	public Node UserNode { get; set; }

	public string HostUserId { get; }

	public string Name { get; private set; }

	public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>()
	{
		{"host", "MRETestBed" },
		{"engine", Engine.GetVersionInfo()["build"] as string }
	};

	//public Vector3? LookAtPosition => (UserNode as Spatial).GlobalTransform.origin;

	public event MWEventHandler BeforeAvatarDestroyed;
	public event MWEventHandler AfterAvatarCreated;

	public HostAppUser(string hostUserId, string name)
	{
		GD.Print($"Creating host app with host user id: {hostUserId}");
		HostUserId = hostUserId;
		Name = name;
	}

	public Node GetAttachPoint(string attachPointName)
	{
		string socketName = $"socket-{attachPointName}";
		Node socket = UserNode.FindNode(socketName);
		if (socket == null)
		{
			socket = UserNode;
		}
		return socket;
	}
}
