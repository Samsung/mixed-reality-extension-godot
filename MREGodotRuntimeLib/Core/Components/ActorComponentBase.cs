// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Godot;
using MixedRealityExtension.Util.GodotHelper;

namespace MixedRealityExtension.Core.Components
{
	internal class ActorComponentBase : Spatial
	{
		internal Actor AttachedActor { get; set; }

		internal virtual void CleanUp()
		{

		}

		internal virtual void SynchronizeComponent()
		{

		}

		private void Start()
		{
			AttachedActor = this.GetChild<Actor>() ??
				throw new NullReferenceException("Node must have an actor node on it if it is going to have an actor component on it.");
		}
	}
}
