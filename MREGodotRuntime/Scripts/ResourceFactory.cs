// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using MixedRealityExtension.PluginInterfaces;

public class ResourceFactory : ILibraryResourceFactory
{
	public Task<Node3D> CreateFromLibrary(string resourceId, Node3D parent)
	{
		//not support
		//var prefab = Resources.Load<Node3D>($"Library/{resourceId}");
		Node3D prefab = null;
		if (prefab == null)
		{
			return Task.FromException<Node3D>(new ArgumentException($"Resource with ID {resourceId} not found"));
		}

		return Task.FromResult<Node3D>((Node3D)prefab.Duplicate());
	}
}
