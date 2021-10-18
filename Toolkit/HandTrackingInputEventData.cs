// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Assets.Scripts.Tools;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public class HandTrackingInputEventData
    {
        public HandTrackingInputEventData(PokeTool pokeTool)
        {
            PokeTool = pokeTool;
        }

        public PokeTool PokeTool { get; set; }
    }
}
