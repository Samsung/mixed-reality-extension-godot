// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Godot;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Interface to implement to react to focus enter/exit.
    /// </summary>
    public interface IMixedRealityFocusHandler
    {
        /// <summary>
        /// The Focus Enter event is raised on this Spatial whenever a TargetTool's focus enters this Parent Actor.
        /// </summary>
        void OnFocusEnter(Spatial inputSource, Node userNode, Spatial oldTarget, Spatial newTarget);

        /// <summary>
        /// The Focus Exit event is raised on this Spatial whenever a TargetTool's focus leaves this  Parent Actor.
        /// </summary>
        void OnFocusExit(Spatial inputSource, Node userNode, Spatial oldTarget, Spatial newTarget);

        public void RegisterFocusEvent(Node obj, Node parent)
        {
            parent.AddUserSignal("focus_enter");
            parent.AddUserSignal("focus_exit");
            parent.Connect("focus_enter", obj, nameof(OnFocusEnter));
            parent.Connect("focus_exit", obj, nameof(OnFocusExit));
        }
    }
}