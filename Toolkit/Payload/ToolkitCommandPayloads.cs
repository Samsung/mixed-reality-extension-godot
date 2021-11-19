// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.Patching.Types;

namespace MixedRealityExtension.Messaging.Payloads
{

	/// <summary>
	/// Engine => App
	/// Instructs the engine to instantiate the Toolkit.
	/// </summary>
	public class CreateFromToolkit : NetworkCommandPayload
	{
		public Guid ActorId { get; set; }
		public ToolkitPatch Toolkit { get; set; }
	}
}
