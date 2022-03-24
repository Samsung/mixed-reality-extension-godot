// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.Toolkit.Payloads
{
	[PayloadType(typeof(CreateFromToolkit), "create-from-toolkit")]
	[PayloadType(typeof(ToolkitUpdate), "toolkit-update")]
	public class ToolkitPayloadTypeRegistry
	{
	}
}
