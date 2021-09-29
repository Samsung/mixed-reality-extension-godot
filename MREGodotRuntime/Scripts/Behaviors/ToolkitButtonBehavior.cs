// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Tools;
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using System;

namespace Assets.Scripts.Behaviors
{
	public class ToolkitButtonBehavior : TargetBehavior, IToolkitButtonBehavior
	{
		public ToolkitButtonBehaviorContext Context => _context as ToolkitButtonBehaviorContext;

		public override Type GetDesiredToolType()
		{
			return typeof(ToolkitButtonTool);
		}
	}
}
