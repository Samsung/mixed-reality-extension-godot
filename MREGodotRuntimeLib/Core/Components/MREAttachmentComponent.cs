// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using Godot;

namespace MixedRealityExtension.Core.Components
{
	internal partial class MREAttachmentComponent : Node3D
	{
		public Guid UserId { get; set; }

		public Actor Actor { get; set; }

		public override void _Process(float delta)
		{
			var scale = Actor.Scale;
			Actor.GlobalTransform = GlobalTransform;
			Actor.Scale = scale;
		}
	}
}
