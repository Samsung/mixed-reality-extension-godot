// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Godot;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Implementation of this interface causes a script to receive notifications of Touch events from HandTrackingInputSources
    /// </summary>
    public interface IMixedRealityTouchHandler
    {
        /// <summary>
        /// When a Touch motion has occurred, this handler receives the event.
        /// </summary>
        void OnTouchStarted(Spatial inputSource, Node userNode, Vector3 point);

        /// <summary>
        /// When a Touch motion ends, this handler receives the event.
        /// </summary>
        void OnTouchCompleted(Spatial inputSource, Node userNode, Vector3 point);

        /// <summary>
        /// When a Touch motion is updated, this handler receives the event.
        /// </summary>
        void OnTouchUpdated(Spatial inputSource, Node userNode, Vector3 point);

        public void RegisterTouchEvent(Node obj, Node parent)
        {
            parent.AddUserSignal("touch_started");
            parent.AddUserSignal("touch_updated");
            parent.AddUserSignal("touch_completed");
            parent.Connect("touch_started", obj, nameof(OnTouchStarted));
            parent.Connect("touch_updated", obj, nameof(OnTouchUpdated));
            parent.Connect("touch_completed", obj, nameof(OnTouchCompleted));
        }
    }
}
