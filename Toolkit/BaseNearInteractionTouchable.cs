// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Godot;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Base class for all NearInteractionTouchables.
    /// </summary>
    /// <remarks>
    /// <para>Add this component to objects to raise touch events when in [PokePointer](xref:Microsoft.MixedReality.Toolkit.Input.PokePointer) proximity.
    /// The object layer must be included of the [PokeLayerMasks](xref:Microsoft.MixedReality.Toolkit.Input.PokePointer.PokeLayerMasks).</para>
    /// </remarks>
    public abstract partial class BaseNearInteractionTouchable : Spatial
    {
        internal Spatial node => GetParent<Spatial>();

        [Export]
        protected TouchableEventType eventsToReceive = TouchableEventType.Touch;

        /// <summary>
        /// The type of event to receive.
        /// </summary>
        public TouchableEventType EventsToReceive { get => eventsToReceive; set => eventsToReceive = value; }

        [Export]
        protected float debounceThreshold = 0.01f;
        /// <summary>
        /// Distance in front of the surface at which you will receive a touch completed event.
        /// </summary>
        /// <remarks>
        /// <para>When the touchable is active and the pointer distance becomes greater than +DebounceThreshold (i.e. in front of the surface),
        /// then the Touch Completed event is raised and the touchable object is released by the pointer.</para>
        /// </remarks>
        public float DebounceThreshold { get => debounceThreshold; set => debounceThreshold = value; }

        internal protected virtual void OnValidate()
        {
            debounceThreshold = Math.Max(debounceThreshold, 0);
        }

        public abstract float DistanceToTouchable(Vector3 samplePoint, out Vector3 normal);
    }
}
