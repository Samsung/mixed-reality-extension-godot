// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace MixedRealityExtension.Core.Components
{
	internal class MREAttachmentComponent : Node
	{
		public Guid UserId { get; set; }

		public Actor Actor { get; set; }
	}
}
