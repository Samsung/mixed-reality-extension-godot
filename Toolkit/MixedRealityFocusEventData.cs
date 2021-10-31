using Assets.Scripts.Tools;
using Godot;
using MixedRealityExtension.Core;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public class MixedRealityFocusEventData : InputEventData
    {
        /// <summary>
        /// The old focused object.
        /// </summary>
        public Spatial OldFocusedObject { get; private set; }

        /// <summary>
        /// The new focused object.
        /// </summary>
        public Spatial NewFocusedObject { get; private set; }

        public MixedRealityFocusEventData(Tool tool, Spatial oldFocusedObject, Spatial newFocusedObject) : base(tool)
        {
            OldFocusedObject = oldFocusedObject;
            NewFocusedObject = newFocusedObject;
        }
    }
}
