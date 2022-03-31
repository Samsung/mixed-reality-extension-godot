// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.
using System;
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Core.Interfaces;

namespace MixedRealityExtension.Behaviors.Actions
{
	public interface IActionHandler
	{
		string ActionName { get; }

		Guid AttachedActorId { get; }

		internal void HandleActionPerforming(IUser user, BaseActionData actionData);

		internal void HandleActionStateChanged(IUser user, ActionState oldState, ActionState newState, BaseActionData actionData);
	}
}
