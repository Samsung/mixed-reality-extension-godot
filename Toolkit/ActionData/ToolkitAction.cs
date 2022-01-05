// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.
using System;
using Godot;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Behaviors.Contexts;
using MixedRealityExtension.Core;
using MixedRealityExtension.App;
using MixedRealityExtension.PluginInterfaces.Behaviors;

namespace Microsoft.MixedReality.Toolkit.UI
{
    public static class ToolkitAction
    {
        public static void RegisterAction(MWActionBase action, string name, Spatial toolkit)
        {
            var actor = toolkit.GetParent<Actor>();
            var toolkitBehaviorContext = ToolkitBehaviorContext.GetToolkitBehaviorContext(actor);
            toolkitBehaviorContext.RegisterAction(action, name);
        }

        /*
         * MRE Action can be registered through BehaviorContext.
         * but the Toolkit action works without behavior.
         * so we don't need to expose this class.
         */
        private class ToolkitBehaviorContext : BehaviorContextBase
        {
            // Class for preventing null exception.
            private static IBehavior dummyBehavior = new DummyBehavior();
            private class DummyBehavior: IBehavior
            {
                public IActor Actor { get; set; }
                public void CleanUp() {}
            }

            public static ToolkitBehaviorContext GetToolkitBehaviorContext(Spatial actorSpatial)
            {
                var actor = actorSpatial as Actor;
                if (actor == null) throw new ArgumentException("actor cannot be a null value.", nameof(actorSpatial));
                var toolkitBehaviorContext = new ToolkitBehaviorContext();
                toolkitBehaviorContext.Initialize(dummyBehavior, new WeakReference<MixedRealityExtensionApp>(actor.App), actor);

                return toolkitBehaviorContext;
            }

            private ToolkitBehaviorContext() { }
            protected override void OnInitialized() { }

            public void RegisterAction(MWActionBase action, string name) => RegisterActionHandler(action, name);
        }
    }
}