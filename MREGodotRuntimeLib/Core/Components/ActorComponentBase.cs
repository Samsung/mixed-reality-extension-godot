// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Godot;

namespace MixedRealityExtension.Core.Components
{
	internal partial class ActorComponentBase : Node3D
	{
		internal Actor AttachedActor { get; set; }

		internal virtual void CleanUp()
		{

		}

		internal virtual void SynchronizeComponent()
		{

		}

		public override void _Ready()
		{
			AttachedActor = GetParent() as Actor ??
				throw new NullReferenceException("Node must have an actor node on it if it is going to have an actor component on it.");
		}
	}
}
