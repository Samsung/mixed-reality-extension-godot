// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Godot;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Interface to implement to react to simple pointer input.
    /// </summary>
    public interface IMixedRealityPointerHandler
    {
        /// <summary>
        /// When a pointer down event is raised, this method is used to pass along the event data to the input handler.
        /// </summary>
        void OnPointerDown(Spatial inputSource, Node userNode, Vector3 point);

        /// <summary>
        /// Called every frame a pointer is down. Can be used to implement drag-like behaviors.
        /// </summary>
        void OnPointerDragged(Spatial inputSource, Node userNode, Vector3 point);

        /// <summary>
        /// When a pointer up event is raised, this method is used to pass along the event data to the input handler.
        /// </summary>
        void OnPointerUp(Spatial inputSource, Node userNode, Vector3 point);

        /// <summary>
        /// When a pointer clicked event is raised, this method is used to pass along the event data to the input handler.
        /// </summary>
        void OnPointerClicked(Spatial inputSource, Node userNode, Vector3 point);

        public void RegisterPointerEvent(Node obj, Node parent)
        {
            parent.AddUserSignal("pointer_down");
            parent.AddUserSignal("pointer_up");
            parent.AddUserSignal("pointer_dragged");
            parent.Connect("pointer_down", obj, nameof(OnPointerDown));
            parent.Connect("pointer_up", obj, nameof(OnPointerUp));
            parent.Connect("pointer_dragged", obj, nameof(OnPointerDragged));
        }
    }
}