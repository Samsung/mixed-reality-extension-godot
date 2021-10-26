// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Assets.Scripts.Tools;
using Godot;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public class TouchInputEventData : InputEventData<Vector3>
    {
        public TouchInputEventData(Tool tool, Vector3 data) : base(tool, data)
        {

        }
    }
}
