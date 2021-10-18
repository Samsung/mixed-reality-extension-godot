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
			var buttonBehavior = actor.Node3D.GetChild<ButtonBehavior>() ?? actor.Node3D.AddNode(new ButtonBehavior() { Name = "ButtonBehavior" });
			buttonBehavior.SetContext(context);
			return buttonBehavior;
		}

		public IPenBehavior GetOrCreatePenBehavior(IActor actor, PenBehaviorContext context)
		{
			var penBehavior = actor.Node3D.GetChild<PenBehavior>() ?? actor.Node3D.AddNode(new PenBehavior() { Name = "PenBehavior" });
			penBehavior.SetContext(context);
			penBehavior.Grabbable = true;
			return penBehavior;
		}

		public ITargetBehavior GetOrCreateTargetBehavior(IActor actor, TargetBehaviorContext context)
		{
			var targetBehavior = actor.Node3D.GetChild<TargetBehavior>() ?? actor.Node3D.AddNode(new TargetBehavior() { Name = "TargetBehavior" });
			targetBehavior.SetContext(context);
			return targetBehavior;
		}
	}
}
