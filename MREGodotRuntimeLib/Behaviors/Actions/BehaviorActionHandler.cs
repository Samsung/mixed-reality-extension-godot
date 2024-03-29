// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
using System;

namespace MixedRealityExtension.Behaviors.Actions
{
	internal sealed class BehaviorActionHandler : IActionHandler
	{
		private readonly BehaviorType _behaviorType;
		private readonly WeakReference<MixedRealityExtensionApp> _appRef;

		public string ActionName { get; }

		public Guid AttachedActorId { get; }

		internal BehaviorActionHandler(
			BehaviorType behaviorType,
			string actionName,
			WeakReference<MixedRealityExtensionApp> appRef,
			Guid attachedActorId)
		{
			_behaviorType = behaviorType;
			_appRef = appRef;
			ActionName = actionName;
			AttachedActorId = attachedActorId;
		}

		void IActionHandler.HandleActionPerforming(IUser user, BaseActionData actionData)
		{
			((IActionHandler)this).HandleActionStateChanged(user, ActionState.Performing, ActionState.Performing, actionData);
		}

		void IActionHandler.HandleActionStateChanged(
			IUser user,
			ActionState oldState,
			ActionState newState,
			BaseActionData actionData)
		{
			MixedRealityExtensionApp app;
			if (!_appRef.TryGetTarget(out app))
			{
				return;
			}

			if (!app.IsInteractableForUser(user))
			{
				return;
			}

			var actionPerformed = new ActionPerformed()
			{
				UserId = user.Id,
				TargetId = AttachedActorId,
				BehaviorType = _behaviorType,
				ActionName = ActionName,
				ActionState = newState,
				ActionData = actionData
			};

			app.EventManager.QueueLateEvent(new BehaviorEvent(actionPerformed));
		}
	}
}
