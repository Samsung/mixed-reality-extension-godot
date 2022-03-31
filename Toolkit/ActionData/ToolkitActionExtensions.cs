// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.
using System;
using Godot;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.App;

namespace Microsoft.MixedReality.Toolkit.UI
{
    public static class ToolkitActionExtensions
    {
        public static void RegisterAction(this Spatial toolkit, MWActionBase action, string name)
        {
            var actor = toolkit.GetParent<IActor>();
            action.Handler = new ActionHandler(name, new WeakReference<IMixedRealityExtensionApp>(actor.App), actor.Id);
        }
    }
}