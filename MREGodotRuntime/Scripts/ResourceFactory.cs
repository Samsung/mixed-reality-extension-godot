// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using MixedRealityExtension.PluginInterfaces;

public class ResourceFactory : ILibraryResourceFactory
{
	public Task<Spatial> CreateFromLibrary(string resourceId, Spatial parent)
	{
		//not support
		//var prefab = Resources.Load<Spatial>($"Library/{resourceId}");
		Spatial prefab = null;
		if (prefab == null)
		{
			return Task.FromException<Spatial>(new ArgumentException($"Resource with ID {resourceId} not found"));
		}

		return Task.FromResult<Spatial>((Spatial)prefab.Duplicate());
	}
}
