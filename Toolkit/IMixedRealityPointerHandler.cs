// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Godot;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Interface to implement to react to simple pointer input.
    /// </summary>
    public interface IMixedRealityPointerHandler : IMixedRealityEventHandler
    {
        /// <summary>
        /// When a pointer down event is raised, this method is used to pass along the event data to the input handler.
        /// </summary>
        [Signal]
        delegate void OnPointerDown(MixedRealityPointerEventData eventData);

        /// <summary>
        /// Called every frame a pointer is down. Can be used to implement drag-like behaviors.
        /// </summary>
        [Signal]
        delegate void OnPointerDragged(MixedRealityPointerEventData eventData);

        /// <summary>
        /// When a pointer up event is raised, this method is used to pass along the event data to the input handler.
        /// </summary>
        [Signal]
        delegate void OnPointerUp(MixedRealityPointerEventData eventData);

        /// <summary>
        /// When a pointer clicked event is raised, this method is used to pass along the event data to the input handler.
        /// </summary>
        [Signal]
        delegate void OnPointerClicked(MixedRealityPointerEventData eventData);
    }
}