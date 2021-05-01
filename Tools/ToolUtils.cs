// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.PluginInterfaces.Behaviors;
using MixedRealityExtension.Util.GodotHelper;
using System.Linq;
using Godot;

namespace Assets.Scripts.Tools
{
	public static class ToolUtils
	{
		public static BehaviorT GetBehavior<BehaviorT>(this Node _this) 
			where BehaviorT : class, IBehavior
		{
			return _this.GetChildren<BehaviorT>().FirstOrDefault();
		}
	}
}
