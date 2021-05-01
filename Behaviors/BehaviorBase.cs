// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using MixedRealityExtension.Util.GodotHelper;
using System;
using System.Linq;
using Godot;

namespace Assets.Scripts.Behaviors
{
	public abstract class BehaviorBase : Node, IBehavior
	{
		public IActor Actor { get; set; }

		public abstract Type GetDesiredToolType();

		public IUser GetMWUnityUser(Node userNode)
		{
			return userNode.GetChildren<IUser>()
				.Where(user => user.AppInstanceId == Actor.AppInstanceId)
				.FirstOrDefault();
		}

		public void CleanUp()
		{
			Free();
		}
	}
}
