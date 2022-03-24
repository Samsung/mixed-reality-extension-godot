// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Messaging.Payloads;

namespace MixedRealityExtension.Toolkit.Payloads
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

	/// <summary>
	/// App => Engine
	/// Payload for when the app wants to update an toolkit with a patch.
	/// </summary>
	public class ToolkitUpdate : NetworkCommandPayload
	{
		public Guid ActorId { get; set; }
		/// <summary>
		/// The toolkit patch to apply to the toolkit associated with the patch.
		/// </summary>
		public ToolkitPatch Toolkit { get; set; }
	}
}
