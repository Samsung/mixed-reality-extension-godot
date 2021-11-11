// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Godot;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Implementation of this interface causes a script to receive notifications of Touch events from HandTrackingInputSources
    /// </summary>
    public interface IMixedRealityTouchHandler : IMixedRealityEventHandler
    {
        /// <summary>
        /// When a Touch motion has occurred, this handler receives the event.
        /// </summary>
        /// <remarks>
        /// A Touch motion is defined as occurring within the bounds of an object (transitive).
        /// </remarks>
        /// <param name="eventData">Contains information about the HandTrackingInputSource.</param>
        [Signal]
        delegate void OnTouchStarted(TouchInputEventData eventData);

        /// <summary>
        /// When a Touch motion ends, this handler receives the event.
        /// </summary>
        /// <remarks>
        /// A Touch motion is defined as occurring within the bounds of an object (transitive).
        /// </remarks>
        /// <param name="eventData">Contains information about the HandTrackingInputSource.</param>
        [Signal]
        delegate void OnTouchCompleted(TouchInputEventData eventData);

        /// <summary>
        /// When a Touch motion is updated, this handler receives the event.
        /// </summary>
        /// <remarks>
        /// A Touch motion is defined as occurring within the bounds of an object (transitive).
        /// </remarks>
        /// <param name="eventData">Contains information about the HandTrackingInputSource.</param>
        [Signal]
        delegate void OnTouchUpdated(TouchInputEventData eventData);
    }
}
