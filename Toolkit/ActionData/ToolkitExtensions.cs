// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using Godot;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.App;
using MixedRealityExtension.Util.GodotHelper;

namespace Microsoft.MixedReality.Toolkit.UI
{
	public static class ToolkitExtensions
	{
		public static void RegisterAction(this IToolkit toolkit, MWActionBase action, string name)
		{
			var actor = toolkit.Parent as IActor;
			action.Handler = new ActionHandler(name, new WeakReference<IMixedRealityExtensionApp>(actor.App), actor.Id);
		}

		public static IUser GetMREUser(this IToolkit toolkit, Node userNode)
		{
			var actor = toolkit.Parent as IActor;
			if (actor == null || userNode == null) return null;
			return userNode.GetChildren<IUser>()
				.Where(user => user.AppInstanceId == actor.AppInstanceId)
				.FirstOrDefault();
		}
	}
}
