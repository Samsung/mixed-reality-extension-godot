// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Godot;

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
			if (AttachedActor == null)
			{
				int childCount = GetChildCount();

				for (int i = 0; i < childCount; i++)
				{
					var child = GetChild<Actor>(i);
					if (child != null)
					{
						AttachedActor = child;
						break;
					}
				}
			}
			if (AttachedActor == null)
				throw new NullReferenceException("Node must have an actor child on it if it is going to have an actor component on it.");
		}
	}
}
