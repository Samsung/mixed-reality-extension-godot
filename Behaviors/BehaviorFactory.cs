// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using MixedRealityExtension.Util.GodotHelper;

namespace Assets.Scripts.Behaviors
{
	public class BehaviorFactory : IBehaviorFactory
	{
		public IButtonBehavior GetOrCreateButtonBehavior(IActor actor, ButtonBehaviorContext context)
		{
			var buttonBehavior = actor.node.GetChild<ButtonBehavior>() ?? actor.node.AddNode(new ButtonBehavior() { Name = "ButtonBehavior" });
			buttonBehavior.SetContext(context);
			return buttonBehavior;
		}

		public IPenBehavior GetOrCreatePenBehavior(IActor actor, PenBehaviorContext context)
		{
			var penBehavior = actor.node.GetChild<PenBehavior>() ?? actor.node.AddNode(new PenBehavior() { Name = "PenBehavior" });
			penBehavior.SetContext(context);
			penBehavior.Grabbable = true;
			return penBehavior;
		}

		public ITargetBehavior GetOrCreateTargetBehavior(IActor actor, TargetBehaviorContext context)
		{
			var targetBehavior = actor.node.GetChild<TargetBehavior>() ?? actor.node.AddNode(new TargetBehavior() { Name = "TargetBehavior" });
			targetBehavior.SetContext(context);
			return targetBehavior;
		}
	}
}
