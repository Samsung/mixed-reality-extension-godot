// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using MixedRealityExtension.Behaviors.Actions;
using Godot;
using System;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public interface IMixedRealityEventHandler
    {
        public static T FindEventHandler<T>(Node node) where T : class
        {
            if (node is T EventHandler)
                return EventHandler;

            foreach (Node child in node.GetChildren())
            {
                var touchableChild = FindEventHandler<T>(child);
                if (touchableChild != null) return touchableChild;
            }
            return null;
        }
    }
}
