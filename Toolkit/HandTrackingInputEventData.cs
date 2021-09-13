// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Godot;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public class HandTrackingInputEventData
    {
        public HandTrackingInputEventData(Spatial controller, Vector3 inputData, Vector3 previousPosition)
        {
            Controller = controller;
            InputData = inputData;
            PreviousPosition = previousPosition;
        }

        public Spatial Controller { get; set; }
        public Vector3 InputData { get; set; }
        public Vector3 PreviousPosition { get; set; }
    }
}
